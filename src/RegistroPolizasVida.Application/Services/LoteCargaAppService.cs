using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.Application.Services;

public sealed class LoteCargaAppService : ILoteCargaAppService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public LoteCargaAppService(IUnitOfWorkFactory unitOfWorkFactory) => _unitOfWorkFactory = unitOfWorkFactory;

    public async Task<LoteCarga> CrearLoteAsync(string nombreArchivoZip, CancellationToken ct = default)
    {
        var lote = new LoteCarga { NombreArchivoZip = nombreArchivoZip };

        using var uow = _unitOfWorkFactory.Crear();
        await uow.Lotes.AgregarAsync(lote, ct);
        await uow.GuardarCambiosAsync(ct);
        return lote;
    }

    public async Task<LoteCarga?> ObtenerEstadoAsync(Guid loteCargaId, CancellationToken ct = default)
    {
        using var uow = _unitOfWorkFactory.Crear();
        return await uow.Lotes.ObtenerPorIdAsync(loteCargaId, ct);
    }

    public async Task<IReadOnlyList<LoteCarga>> ObtenerRecientesAsync(int cantidad = 20, CancellationToken ct = default)
    {
        using var uow = _unitOfWorkFactory.Crear();
        return await uow.Lotes.ObtenerRecientesAsync(cantidad, ct);
    }
}