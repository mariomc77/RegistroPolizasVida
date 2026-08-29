using Microsoft.EntityFrameworkCore;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.DataAccess.Repositorios;

public sealed class LoteCargaRepository : ILoteCargaRepository
{
    private readonly RegistroPolizasVidaDbContext _db;
    public LoteCargaRepository(RegistroPolizasVidaDbContext db) => _db = db;

    public Task AgregarAsync(LoteCarga lote, CancellationToken ct = default)
    {
        _db.Lotes.Add(lote);
        return Task.CompletedTask;
    }

    public Task<LoteCarga?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Lotes
            .Include(l => l.Archivos).ThenInclude(a => a.Errores)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<LoteCarga>> ObtenerRecientesAsync(int cantidad = 20, CancellationToken ct = default) =>
        await _db.Lotes
            .OrderByDescending(l => l.FechaCarga)
            .Take(cantidad)
            .ToListAsync(ct);

    /// <summary>
    /// No-op cuando <paramref name="lote"/> ya viene siendo rastreado por este mismo
    /// contexto (el caso normal: <see cref="Application.Services.BatchProcessingService"/>
    /// usa una única unidad de trabajo desde que crea/obtiene el lote hasta que lo
    /// guarda al final, así que EF Core ya conoce todos los cambios). Si llegara un
    /// lote desconectado de otro contexto, se adjunta como modificado.
    /// </summary>
    public Task ActualizarAsync(LoteCarga lote, CancellationToken ct = default)
    {
        if (_db.Entry(lote).State == EntityState.Detached)
        {
            _db.Lotes.Attach(lote);
            _db.Entry(lote).State = EntityState.Modified;
        }
        return Task.CompletedTask;
    }

    /// <summary>AddRange marca cada ArchivoLote (y sus ErrorProcesamiento) como Agregado explicitamente.</summary>
    public Task AgregarArchivosAsync(IEnumerable<ArchivoLote> archivos, CancellationToken ct = default)
    {
        _db.ArchivosLote.AddRange(archivos);
        return Task.CompletedTask;
    }
}