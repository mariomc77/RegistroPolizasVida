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

/// <summary>
/// Implementación de referencia del pipeline de procesamiento por lotes.
///
/// Responde directamente a las preguntas del reto:
///  - Aprovecha todos los núcleos: los archivos .XML del .ZIP se procesan con
///    <see cref="Parallel.ForEachAsync{TSource}"/> con grado de paralelismo igual
///    a <see cref="Environment.ProcessorCount"/>.
///  - Mejora la capacidad de respuesta: todo el método es asíncrono (E/S de disco,
///    de base de datos) y se ejecuta fuera del hilo de la petición HTTP (lo invoca
///    el worker de la cola de tareas, ver <see cref="BackgroundQueue.IBackgroundTaskQueue"/>).
///  - Reutiliza código: cada archivo pasa por el mismo validador de esquema, el
///    mismo parser y el mismo validador de reglas de negocio, sin importar si viene
///    solo o junto a otros cinco en el mismo .ZIP.
/// </summary>
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

    public async Task<LoteCarga> ProcesarLoteAsync(Guid loteCargaId, byte[] contenidoZip, CancellationToken ct = default)
    {
        using var uowPrincipal = _unitOfWorkFactory.Crear();
        var lote = await uowPrincipal.Lotes.ObtenerPorIdAsync(loteCargaId, ct)
            ?? throw new InvalidOperationException($"No existe el lote {loteCargaId}. Debe crearse antes de encolar su procesamiento.");

        try
        {
            IReadOnlyList<EntradaZip> entradas;
            using (var flujoZip = new MemoryStream(contenidoZip))
            {
                entradas = _extractor.ExtraerArchivosXml(flujoZip);
            }

            lote.Estado = EstadoLote.Procesando;
            lote.TotalArchivos = entradas.Count;
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

            var resultados = new ConcurrentBag<ArchivoLote>();
            var archivosProcesados = 0;

            var opciones = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(entradas, opciones, async (entrada, tokenTarea) =>
            {
                var resultadoArchivo = await ProcesarArchivoAsync(entrada, loteCargaId, tokenTarea);
                resultados.Add(resultadoArchivo);

                var completados = Interlocked.Increment(ref archivosProcesados);
                lote.ArchivosProcesados = completados;
                await _notificador.NotificarProgresoAsync(lote, tokenTarea);
            });

            lote.Archivos.AddRange(resultados);
            await uowPrincipal.Lotes.AgregarArchivosAsync(resultados, ct);
            lote.TotalPolizas = lote.Archivos.Sum(a => a.CantidadPolizas);
            lote.PolizasInsertadas = lote.Archivos.Sum(a => a.PolizasInsertadas);
            lote.PolizasActualizadas = lote.Archivos.Sum(a => a.PolizasActualizadas);
            lote.PolizasConError = lote.Archivos.Sum(a => a.PolizasConError);
            lote.FechaFinProcesamiento = DateTime.UtcNow;

            var huboExito = lote.Archivos.Any(a => a.Estado is EstadoArchivo.Procesado or EstadoArchivo.ProcesadoConErrores);
            var huboError = lote.Archivos.Any(a => a.Estado is not EstadoArchivo.Procesado);
            lote.Estado = (huboExito, huboError) switch
            {
                (true, false) => EstadoLote.Completado,
                (true, true) => EstadoLote.CompletadoConErrores,
                (false, _) => EstadoLote.Fallido
            };

            await GuardarLoteFinalAsync(uowPrincipal, lote, ct);
            await _notificador.NotificarFinalizacionAsync(lote, ct);
            return lote;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error inesperado procesando el lote {LoteId}", loteCargaId);
            lote.Estado = EstadoLote.Fallido;
            lote.FechaFinProcesamiento = DateTime.UtcNow;
            try
            {
                await GuardarLoteFinalAsync(uowPrincipal, lote, ct);
            }
            catch (Exception exGuardado)
            {
                _logger.LogError(exGuardado, "No se pudo guardar el estado Fallido del lote {LoteId}", loteCargaId);
            }
            await _notificador.NotificarFinalizacionAsync(lote, ct);
            return lote;
        }
    }

    private static async Task GuardarLoteFinalAsync(IUnitOfWork uow, LoteCarga lote, CancellationToken ct)
    {
        await uow.Lotes.ActualizarAsync(lote, ct);
        await uow.GuardarCambiosAsync(ct);
    }

    /// <summary>
    /// Procesa un único archivo .XML de principio a fin: esquema ? parseo ? reglas de
    /// negocio ? persistencia. Usa su propia unidad de trabajo (su propio contexto de
    /// base de datos) porque corre concurrentemente con el resto de archivos del lote.
    /// </summary>
    private async Task<ArchivoLote> ProcesarArchivoAsync(EntradaZip entrada, Guid loteCargaId, CancellationToken ct)
    {
        var archivoLote = new ArchivoLote
        {
            LoteCargaId = loteCargaId,
            NombreArchivo = entrada.NombreArchivo
        };

        // 1) Validación de esquema XSD.
        ResultadoValidacion resultadoEsquema;
        using (var flujo = new MemoryStream(entrada.Contenido))
        {
            resultadoEsquema = _validadorEsquema.Validar(flujo);
        }

        if (!resultadoEsquema.EsValido)
        {
            archivoLote.Estado = EstadoArchivo.InvalidoEsquema;
            AgregarErrores(archivoLote, resultadoEsquema.Hallazgos);
            return archivoLote;
        }

        // 2) Parseo a objetos de dominio (ya validado el esquema; solo debería fallar
        //    ante algo que el XSD no puede expresar, p. ej. un enum con valor de más).
        LotePolizasXml loteXml;
        try
        {
            using var flujo = new MemoryStream(entrada.Contenido);
            loteXml = _parser.Parsear(flujo);
        }
        catch (FormatException ex)
        {
            archivoLote.Estado = EstadoArchivo.InvalidoEsquema;
            AgregarErrores(archivoLote, new[] { new HallazgoValidacion(TipoError.Formato, ex.Message) });
            return archivoLote;
        }

        archivoLote.CantidadPolizas = loteXml.Polizas.Count;

        // 3) Reglas de negocio a nivel de archivo (p. ej. números de póliza repetidos
        //    dentro del mismo XML): si fallan, no se procesa ninguna póliza del archivo
        //    porque no hay forma no ambigua de decidir cuál copia es la correcta.
        var resultadoLote = _validadorNegocio.ValidarLote(loteXml);
        if (!resultadoLote.EsValido)
        {
            archivoLote.Estado = EstadoArchivo.ErrorNegocio;
            archivoLote.PolizasConError = archivoLote.CantidadPolizas;
            AgregarErrores(archivoLote, resultadoLote.Hallazgos);
            return archivoLote;
        }

        // 4) Reglas de negocio por póliza + persistencia (upsert) de las válidas.
        using var uow = _unitOfWorkFactory.Crear();

        foreach (var poliza in loteXml.Polizas)
        {
            var resultadoPoliza = _validadorNegocio.ValidarPoliza(poliza);
            if (!resultadoPoliza.EsValido)
            {
                archivoLote.PolizasConError++;
                AgregarErrores(archivoLote, resultadoPoliza.Hallazgos);
                continue;
            }

            try
            {
                var accion = await uow.Polizas.GuardarAsync(poliza, loteCargaId, ct);
                if (accion == AccionPersistencia.Insertada) archivoLote.PolizasInsertadas++;
                else archivoLote.PolizasActualizadas++;
            }
            catch (Exception ex)
            {
                archivoLote.PolizasConError++;
                AgregarErrores(archivoLote, new[]
                {
                    new HallazgoValidacion(TipoError.Persistencia,
                        $"No se pudo guardar la póliza: {ex.Message}", poliza.NumeroPoliza)
                });
            }
        }

        try
        {
            await uow.GuardarCambiosAsync(ct);
        }
        catch (Exception ex)
        {
            // Si el commit del archivo completo falla (p. ej. una restricción de la base
            // de datos), se reporta a nivel de archivo: no sabemos, sin más información,
            // cuál póliza específica fue la causante.
            _logger.LogError(ex, "Error guardando cambios del archivo {Archivo} del lote {LoteId}", entrada.NombreArchivo, loteCargaId);
            archivoLote.PolizasConError = archivoLote.CantidadPolizas;
            archivoLote.PolizasInsertadas = 0;
            archivoLote.PolizasActualizadas = 0;
            AgregarErrores(archivoLote, new[]
            {
                new HallazgoValidacion(TipoError.Persistencia, $"Error al confirmar los cambios en base de datos: {ex.Message}")
            });
            archivoLote.Estado = EstadoArchivo.ErrorNegocio;
            return archivoLote;
        }

        archivoLote.Estado = archivoLote.PolizasConError == 0
            ? EstadoArchivo.Procesado
            : EstadoArchivo.ProcesadoConErrores;

        return archivoLote;
    }

    private static void AgregarErrores(ArchivoLote archivoLote, IEnumerable<HallazgoValidacion> hallazgos)
    {
        foreach (var h in hallazgos)
        {
            archivoLote.Errores.Add(new ErrorProcesamiento
            {
                ArchivoLoteId = archivoLote.Id,
                NumeroPoliza = h.NumeroPoliza,
                Tipo = h.Tipo,
                Mensaje = h.Mensaje,
                Linea = h.Linea
            });
        }
    }
}