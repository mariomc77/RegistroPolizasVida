using Microsoft.EntityFrameworkCore;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.DataAccess.Repositorios;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly RegistroPolizasVidaDbContext _db;
    private bool _disposed;

    public IPolizaRepository Polizas { get; }
    public ILoteCargaRepository Lotes { get; }

    public UnitOfWork(RegistroPolizasVidaDbContext db)
    {
        _db = db;
        Polizas = new PolizaRepository(db);
        Lotes = new LoteCargaRepository(db);
    }

    public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public void Dispose()
    {
        if (_disposed) return;
        _db.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Crea una <see cref="UnitOfWork"/> con un <see cref="RegistroPolizasVidaDbContext"/>
/// NUEVO en cada llamada (nunca reutiliza uno). Esto es lo que permite que
/// <c>BatchProcessingService</c> procese varios archivos .XML de un mismo lote en
/// paralelo con <c>Parallel.ForEachAsync</c>: cada tarea concurrente pide su propia
/// unidad de trabajo y por lo tanto su propio contexto, evitando el uso concurrente
/// de un mismo DbContext (que EF Core no admite).
/// </summary>
public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly DbContextOptions<RegistroPolizasVidaDbContext> _opciones;
    public UnitOfWorkFactory(DbContextOptions<RegistroPolizasVidaDbContext> opciones) => _opciones = opciones;

    public IUnitOfWork Crear() => new UnitOfWork(new RegistroPolizasVidaDbContext(_opciones));
}