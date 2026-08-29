using Microsoft.EntityFrameworkCore;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.DataAccess.Repositorios;

public sealed class PolizaRepository : IPolizaRepository
{
    private readonly RegistroPolizasVidaDbContext _db;
    public PolizaRepository(RegistroPolizasVidaDbContext db) => _db = db;

    public Task<Poliza?> ObtenerPorNumeroAsync(string numeroPoliza, CancellationToken ct = default) =>
        _db.Polizas
            .Include(p => p.Asegurados).ThenInclude(a => a.Beneficiarios)
            .FirstOrDefaultAsync(p => p.NumeroPoliza == numeroPoliza, ct);

    /// <summary>
    /// Upsert por número de póliza. Si ya existe, se reemplazan sus datos completos
    /// (tomador, asegurados y beneficiarios) -- este es el mecanismo que hace que
    /// subir un lote posterior con el mismo número de póliza (ver polizas_lote_05.xml,
    /// que reemplaza la póliza cargada en polizas_lote_01.xml) actualice en vez de
    /// duplicar. La lógica es idéntica a la validada en
    /// tools/RegistroPolizasVida.SelfCheck/InMemory/InMemoryUnitOfWork.cs.
    /// </summary>
    public async Task<AccionPersistencia> GuardarAsync(Poliza poliza, Guid loteCargaId, CancellationToken ct = default)
    {
        var existente = await _db.Polizas
            .Include(p => p.Asegurados).ThenInclude(a => a.Beneficiarios)
            .FirstOrDefaultAsync(p => p.NumeroPoliza == poliza.NumeroPoliza, ct);

        var ahora = DateTime.UtcNow;

        if (existente is null)
        {
            poliza.LoteCargaOrigenId = loteCargaId;
            poliza.LoteCargaUltimaActualizacionId = loteCargaId;
            poliza.FechaCreacion = ahora;
            poliza.FechaActualizacion = ahora;
            poliza.VersionActualizacion = 1;

            _db.Polizas.Add(poliza);
            return AccionPersistencia.Insertada;
        }

        existente.Tipo = poliza.Tipo;
        existente.FechaEmision = poliza.FechaEmision;
        existente.FechaVencimiento = poliza.FechaVencimiento;
        existente.MontoCobertura = poliza.MontoCobertura;
        existente.Moneda = poliza.Moneda;
        existente.Tomador = poliza.Tomador;

        // Reasignar la colección completa: como la relación Asegurado->Poliza es
        // obligatoria, EF Core aplica "eliminar huérfanos" sobre los asegurados (y,
        // en cascada, sus beneficiarios) que ya no están en la lista nueva, y agrega
        // los que sí -- sin necesidad de comparar entidad por entidad a mano.
        existente.Asegurados.Clear();
        foreach (var asegurado in poliza.Asegurados)
        {
            asegurado.PolizaNumero = poliza.NumeroPoliza;
        }
        _db.Asegurados.AddRange(poliza.Asegurados);
        existente.Asegurados.AddRange(poliza.Asegurados);

        existente.LoteCargaUltimaActualizacionId = loteCargaId;
        existente.FechaActualizacion = ahora;
        existente.VersionActualizacion += 1;

        return AccionPersistencia.Actualizada;
    }
}