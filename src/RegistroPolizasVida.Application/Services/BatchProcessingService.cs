using System.Collections.Concurrent;
using RegistroPolizasVida.Application.Common;
using RegistroPolizasVida.Application.Dtos;
using RegistroPolizasVida.Application.Notifications;
using RegistroPolizasVida.Application.Parsing;
using RegistroPolizasVida.Application.Validation;
using RegistroPolizasVida.Application.Zip;
using RegistroPolizasVida.Domain.Common;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.Application.Services;

public sealed class BatchProcessingService : IBatchProcessingService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IZipBatchExtractor _extractor;
    private readonly IXsdBatchValidator _validadorEsquema;
    private readonly IPolizaXmlParser _parser;
    private readonly IPolizaBusinessRuleValidator _validadorNegocio;
    private readonly IProcesamientoNotifier _notificador;
    private readonly IPipelineLogger<BatchProcessingService> _logger;

    public BatchProcessingService(
        IUnitOfWorkFactory unitOfWorkFactory,
        IZipBatchExtractor extractor,
        IXsdBatchValidator validadorEsquema,
        IPolizaXmlParser parser,
        IPolizaBusinessRuleValidator validadorNegocio,
        IProcesamientoNotifier notificador,
        IPipelineLogger<BatchProcessingService> logger)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _extractor = extractor;
        _validadorEsquema = validadorEsquema;
        _parser = parser;
        _validadorNegocio = validadorNegocio;
        _notificador = notificador;
        _logger = logger;
    }

    public async Task<LoteCarga> ProcesarLoteAsync(
        Guid loteCargaId,
        byte[] contenidoZip,
        CancellationToken ct = default)
    {
        using var uowPrincipal = _unitOfWorkFactory.Crear();

        var lote = await uowPrincipal.Lotes.ObtenerPorIdAsync(loteCargaId, ct)
            ?? throw new InvalidOperationException(
                $"No existe el lote {loteCargaId}. Debe crearse antes de encolar su procesamiento.");

        try
        {
            IReadOnlyList<EntradaZip> entradas;

            using (var flujoZip = new MemoryStream(contenidoZip))
            {
                entradas = _extractor.ExtraerArchivosXml(flujoZip);
            }

            lote.Estado = EstadoLote.Procesando;
            lote.TotalArchivos = entradas.Count;
            lote.ArchivosProcesados = 0;
            lote.FechaInicioProcesamiento = DateTime.UtcNow;

            await uowPrincipal.Lotes.ActualizarAsync(lote, ct);
            await uowPrincipal.GuardarCambiosAsync(ct);
            await _notificador.NotificarInicioAsync(lote, ct);

            if (entradas.Count == 0)
            {
                lote.Estado = EstadoLote.Fallido;
                lote.FechaFinProcesamiento = DateTime.UtcNow;

                await GuardarLoteFinalAsync(uowPrincipal, lote, ct);
                await _notificador.NotificarFinalizacionAsync(lote, ct);

                return lote;
            }

            var preparados = new ConcurrentDictionary<int, ArchivoPreparado>();

            var entradasIndexadas = entradas
                .Select((entrada, indice) => new EntradaIndexada(indice, entrada))
                .ToArray();

            var opciones = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(
                entradasIndexadas,
                opciones,
                (item, tokenTarea) =>
                {
                    tokenTarea.ThrowIfCancellationRequested();

                    var preparado = PrepararArchivo(
                        item.Indice,
                        item.Entrada,
                        loteCargaId);

                    preparados[item.Indice] = preparado;

                    return ValueTask.CompletedTask;
                });

            var ordenPersistencia = preparados.Values
                .OrderBy(p => p.LoteXml is null ? 1 : 0)
                .ThenBy(p => p.LoteXml?.FechaLote ?? DateOnly.MaxValue)
                .ThenBy(
                    p => p.LoteXml?.LoteIdXml ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Indice)
                .ToList();

            var archivosProcesados = 0;

            foreach (var preparado in ordenPersistencia)
            {
                ct.ThrowIfCancellationRequested();

                if (preparado.PuedePersistir)
                {
                    await PersistirArchivoAsync(
                        preparado,
                        loteCargaId,
                        ct);
                }

                archivosProcesados++;

                lote.ArchivosProcesados = archivosProcesados;

                await _notificador.NotificarProgresoAsync(
                    lote,
                    ct);
            }

            var resultados = preparados.Values
                .OrderBy(p => p.Indice)
                .Select(p => p.Archivo)
                .ToList();

            lote.Archivos.AddRange(resultados);

            await uowPrincipal.Lotes.AgregarArchivosAsync(
                resultados,
                ct);

            lote.TotalPolizas = resultados.Sum(a => a.CantidadPolizas);
            lote.PolizasInsertadas = resultados.Sum(a => a.PolizasInsertadas);
            lote.PolizasActualizadas = resultados.Sum(a => a.PolizasActualizadas);
            lote.PolizasConError = resultados.Sum(a => a.PolizasConError);
            lote.FechaFinProcesamiento = DateTime.UtcNow;

            var huboExito = resultados.Any(
                a => a.Estado is EstadoArchivo.Procesado
                    or EstadoArchivo.ProcesadoConErrores);

            var huboError = resultados.Any(
                a => a.Estado is not EstadoArchivo.Procesado);

            lote.Estado = (huboExito, huboError) switch
            {
                (true, false) => EstadoLote.Completado,
                (true, true) => EstadoLote.CompletadoConErrores,
                (false, _) => EstadoLote.Fallido
            };

            await GuardarLoteFinalAsync(
                uowPrincipal,
                lote,
                ct);

            await _notificador.NotificarFinalizacionAsync(
                lote,
                ct);

            return lote;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error inesperado procesando el lote {LoteId}",
                loteCargaId);

            lote.Estado = EstadoLote.Fallido;
            lote.FechaFinProcesamiento = DateTime.UtcNow;

            try
            {
                await GuardarLoteFinalAsync(
                    uowPrincipal,
                    lote,
                    ct);
            }
            catch (Exception exGuardado)
            {
                _logger.LogError(
                    exGuardado,
                    "No se pudo guardar el estado Fallido del lote {LoteId}",
                    loteCargaId);
            }

            await _notificador.NotificarFinalizacionAsync(
                lote,
                ct);

            return lote;
        }
    }

    private ArchivoPreparado PrepararArchivo(
        int indice,
        EntradaZip entrada,
        Guid loteCargaId)
    {
        var archivoLote = new ArchivoLote
        {
            LoteCargaId = loteCargaId,
            NombreArchivo = entrada.NombreArchivo
        };

        ResultadoValidacion resultadoEsquema;

        using (var flujo = new MemoryStream(entrada.Contenido))
        {
            resultadoEsquema = _validadorEsquema.Validar(flujo);
        }

        if (!resultadoEsquema.EsValido)
        {
            archivoLote.Estado = EstadoArchivo.InvalidoEsquema;

            AgregarErrores(
                archivoLote,
                resultadoEsquema.Hallazgos);

            return new ArchivoPreparado(
                indice,
                archivoLote,
                null,
                new List<Poliza>(),
                false);
        }

        LotePolizasXml loteXml;

        try
        {
            using var flujo = new MemoryStream(entrada.Contenido);

            loteXml = _parser.Parsear(flujo);
        }
        catch (FormatException ex)
        {
            archivoLote.Estado = EstadoArchivo.InvalidoEsquema;

            AgregarErrores(
                archivoLote,
                new[]
                {
                    new HallazgoValidacion(
                        TipoError.Formato,
                        ex.Message)
                });

            return new ArchivoPreparado(
                indice,
                archivoLote,
                null,
                new List<Poliza>(),
                false);
        }

        archivoLote.CantidadPolizas = loteXml.Polizas.Count;

        var resultadoLote = _validadorNegocio.ValidarLote(loteXml);

        if (!resultadoLote.EsValido)
        {
            archivoLote.Estado = EstadoArchivo.ErrorNegocio;
            archivoLote.PolizasConError = archivoLote.CantidadPolizas;

            AgregarErrores(
                archivoLote,
                resultadoLote.Hallazgos);

            return new ArchivoPreparado(
                indice,
                archivoLote,
                loteXml,
                new List<Poliza>(),
                false);
        }

        var polizasValidas = new List<Poliza>();

        foreach (var poliza in loteXml.Polizas)
        {
            var resultadoPoliza =
                _validadorNegocio.ValidarPoliza(poliza);

            if (!resultadoPoliza.EsValido)
            {
                archivoLote.PolizasConError++;

                AgregarErrores(
                    archivoLote,
                    resultadoPoliza.Hallazgos);

                continue;
            }

            polizasValidas.Add(poliza);
        }

        return new ArchivoPreparado(
            indice,
            archivoLote,
            loteXml,
            polizasValidas,
            true);
    }

    private async Task PersistirArchivoAsync(
        ArchivoPreparado preparado,
        Guid loteCargaId,
        CancellationToken ct)
    {
        var archivoLote = preparado.Archivo;

        if (preparado.PolizasValidas.Count == 0)
        {
            archivoLote.Estado =
                archivoLote.PolizasConError == 0
                    ? EstadoArchivo.Procesado
                    : EstadoArchivo.ProcesadoConErrores;

            return;
        }

        using var uow = _unitOfWorkFactory.Crear();

        foreach (var poliza in preparado.PolizasValidas)
        {
            try
            {
                var accion = await uow.Polizas.GuardarAsync(
                    poliza,
                    loteCargaId,
                    ct);

                if (accion == AccionPersistencia.Insertada)
                {
                    archivoLote.PolizasInsertadas++;
                }
                else
                {
                    archivoLote.PolizasActualizadas++;
                }
            }
            catch (Exception ex)
            {
                archivoLote.PolizasConError++;

                AgregarErrores(
                    archivoLote,
                    new[]
                    {
                        new HallazgoValidacion(
                            TipoError.Persistencia,
                            $"No se pudo guardar la póliza: {ex.Message}",
                            poliza.NumeroPoliza)
                    });
            }
        }

        try
        {
            await uow.GuardarCambiosAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error guardando cambios del archivo {Archivo} del lote {LoteId}",
                archivoLote.NombreArchivo,
                loteCargaId);

            archivoLote.PolizasConError =
                archivoLote.CantidadPolizas;

            archivoLote.PolizasInsertadas = 0;
            archivoLote.PolizasActualizadas = 0;

            AgregarErrores(
                archivoLote,
                new[]
                {
                    new HallazgoValidacion(
                        TipoError.Persistencia,
                        $"Error al confirmar los cambios en base de datos: {ex.Message}")
                });

            archivoLote.Estado =
                EstadoArchivo.ErrorNegocio;

            return;
        }

        archivoLote.Estado =
            archivoLote.PolizasConError == 0
                ? EstadoArchivo.Procesado
                : EstadoArchivo.ProcesadoConErrores;
    }

    private static async Task GuardarLoteFinalAsync(
        IUnitOfWork uow,
        LoteCarga lote,
        CancellationToken ct)
    {
        await uow.Lotes.ActualizarAsync(lote, ct);
        await uow.GuardarCambiosAsync(ct);
    }

    private static void AgregarErrores(
        ArchivoLote archivoLote,
        IEnumerable<HallazgoValidacion> hallazgos)
    {
        foreach (var hallazgo in hallazgos)
        {
            archivoLote.Errores.Add(
                new ErrorProcesamiento
                {
                    ArchivoLoteId = archivoLote.Id,
                    NumeroPoliza = hallazgo.NumeroPoliza,
                    Tipo = hallazgo.Tipo,
                    Mensaje = hallazgo.Mensaje,
                    Linea = hallazgo.Linea
                });
        }
    }

    private sealed record EntradaIndexada(
        int Indice,
        EntradaZip Entrada);

    private sealed record ArchivoPreparado(
        int Indice,
        ArchivoLote Archivo,
        LotePolizasXml? LoteXml,
        List<Poliza> PolizasValidas,
        bool PuedePersistir);
}