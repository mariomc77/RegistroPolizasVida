using Microsoft.AspNetCore.SignalR;
using RegistroPolizasVida.Application.Notifications;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Web.Hubs;

namespace RegistroPolizasVida.Web.Notifications;

/// <summary>Forma ligera y serializable que viaja por SignalR (evita mandar el grafo completo de EF, con sus referencias circulares).</summary>
public sealed record LoteEstadoPayload(
    Guid Id,
    string NombreArchivoZip,
    string Estado,
    int TotalArchivos,
    int ArchivosProcesados,
    int TotalPolizas,
    int PolizasInsertadas,
    int PolizasActualizadas,
    int PolizasConError,
    double? DuracionSegundos)
{
    public static LoteEstadoPayload DesdeEntidad(LoteCarga lote) => new(
        lote.Id, lote.NombreArchivoZip, lote.Estado.ToString(),
        lote.TotalArchivos, lote.ArchivosProcesados,
        lote.TotalPolizas, lote.PolizasInsertadas, lote.PolizasActualizadas, lote.PolizasConError,
        lote.DuracionSegundos);
}

/// <summary>
/// Implementación de <see cref="IProcesamientoNotifier"/> sobre SignalR: es la pieza
/// que le "avisa" al navegador que subió el lote cuando el procesamiento termina,
/// sin que este tenga que estar preguntando (sondeando) constantemente.
/// </summary>
public sealed class SignalRProcesamientoNotifier : IProcesamientoNotifier
{
    private readonly IHubContext<ProcesamientoHub> _hub;
    public SignalRProcesamientoNotifier(IHubContext<ProcesamientoHub> hub) => _hub = hub;

    public Task NotificarInicioAsync(LoteCarga lote, CancellationToken ct = default) =>
        Enviar("LoteIniciado", lote, ct);

    public Task NotificarProgresoAsync(LoteCarga lote, CancellationToken ct = default) =>
        Enviar("LoteProgreso", lote, ct);

    public Task NotificarFinalizacionAsync(LoteCarga lote, CancellationToken ct = default) =>
        Enviar("LoteFinalizado", lote, ct);

    private Task Enviar(string evento, LoteCarga lote, CancellationToken ct) =>
        _hub.Clients.Group(ProcesamientoHub.GrupoDeLote(lote.Id))
            .SendAsync(evento, LoteEstadoPayload.DesdeEntidad(lote), ct);
}