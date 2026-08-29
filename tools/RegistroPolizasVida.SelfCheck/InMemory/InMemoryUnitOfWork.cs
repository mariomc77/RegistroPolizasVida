using System.Collections.Concurrent;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.SelfCheck.InMemory;

/// <summary>
/// Implementación en memoria de todo el contrato de persistencia del dominio
/// (IUnitOfWork / IPolizaRepository / ILoteCargaRepository), usada SOLO para poder
/// ejecutar el pipeline completo (BatchProcessingService) sin una base de datos real
/// y así verificar la lógica de negocio contra los .XML de ejemplo del reto.
/// La implementación real (con SQL Server vía EF Core) vive en el proyecto
/// RegistroPolizasVida.DataAccess.
/// </summary>
public sealed class InMemoryStore
{
    public ConcurrentDictionary<string, Poliza> Polizas { get; } = new();
    public ConcurrentDictionary<Guid, LoteCarga> Lotes { get; } = new();
}

public sealed class InMemoryUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly InMemoryStore _store;
    public InMemoryUnitOfWorkFactory(InMemoryStore store) => _store = store;
    public IUnitOfWork Crear() => new InMemoryUnitOfWork(_store);
}

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public IPolizaRepository Polizas { get; }
    public ILoteCargaRepository Lotes { get; }

    public InMemoryUnitOfWork(InMemoryStore store)
    {
        Polizas = new InMemoryPolizaRepository(store);
        Lotes = new InMemoryLoteCargaRepository(store);
    }

    public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => Task.FromResult(0);
    public void Dispose() { }
}

file sealed class InMemoryPolizaRepository : IPolizaRepository
{
    private readonly InMemoryStore _store;
    public InMemoryPolizaRepository(InMemoryStore store) => _store = store;

    public Task<Poliza?> ObtenerPorNumeroAsync(string numeroPoliza, CancellationToken ct = default) =>
        Task.FromResult(_store.Polizas.TryGetValue(numeroPoliza, out var p) ? p : null);

    public Task<AccionPersistencia> GuardarAsync(Poliza poliza, Guid loteCargaId, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var accion = AccionPersistencia.Insertada;

        _store.Polizas.AddOrUpdate(
            poliza.NumeroPoliza,
            _ =>
            {
                poliza.LoteCargaOrigenId = loteCargaId;
                poliza.LoteCargaUltimaActualizacionId = loteCargaId;
                poliza.FechaCreacion = ahora;
                poliza.FechaActualizacion = ahora;
                poliza.VersionActualizacion = 1;
                return poliza;
            },
            (_, existente) =>
            {
                accion = AccionPersistencia.Actualizada;
                poliza.LoteCargaOrigenId = existente.LoteCargaOrigenId;
                poliza.LoteCargaUltimaActualizacionId = loteCargaId;
                poliza.FechaCreacion = existente.FechaCreacion;
                poliza.FechaActualizacion = ahora;
                poliza.VersionActualizacion = existente.VersionActualizacion + 1;
                return poliza;
            });

        return Task.FromResult(accion);
    }
}

file sealed class InMemoryLoteCargaRepository : ILoteCargaRepository
{
    private readonly InMemoryStore _store;
    public InMemoryLoteCargaRepository(InMemoryStore store) => _store = store;

    public Task AgregarAsync(LoteCarga lote, CancellationToken ct = default)
    {
        _store.Lotes[lote.Id] = lote;
        return Task.CompletedTask;
    }

    public Task<LoteCarga?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.Lotes.TryGetValue(id, out var l) ? l : null);

    public Task<IReadOnlyList<LoteCarga>> ObtenerRecientesAsync(int cantidad = 20, CancellationToken ct = default) =>
        Task.FromResult((IReadOnlyList<LoteCarga>)_store.Lotes.Values
            .OrderByDescending(l => l.FechaCarga)
            .Take(cantidad)
            .ToList());

    public Task ActualizarAsync(LoteCarga lote, CancellationToken ct = default)
    {
        _store.Lotes[lote.Id] = lote;
        return Task.CompletedTask;
    }

    public Task AgregarArchivosAsync(IEnumerable<ArchivoLote> archivos, CancellationToken ct = default) => Task.CompletedTask;
}