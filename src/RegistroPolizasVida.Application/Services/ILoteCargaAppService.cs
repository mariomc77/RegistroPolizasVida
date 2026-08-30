using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.Application.Services;

/// <summary>
/// Operaciones de consulta y creación de lotes que expone la capa de aplicación a
/// la capa Web -- deliberadamente separadas de <see cref="IBatchProcessingService"/>,
/// que es responsable únicamente del procesamiento pesado.
/// </summary>
public interface ILoteCargaAppService
{
    /// <summary>Crea el registro de seguimiento del lote en estado Pendiente, antes de encolar su procesamiento.</summary>
    Task<LoteCarga> CrearLoteAsync(string nombreArchivoZip, CancellationToken ct = default);

    Task<LoteCarga?> ObtenerEstadoAsync(Guid loteCargaId, CancellationToken ct = default);

    Task<IReadOnlyList<LoteCarga>> ObtenerRecientesAsync(int cantidad = 20, CancellationToken ct = default);
}