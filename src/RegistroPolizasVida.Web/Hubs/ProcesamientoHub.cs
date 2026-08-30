using Microsoft.AspNetCore.SignalR;

namespace RegistroPolizasVida.Web.Hubs;

/// <summary>
/// Canal en tiempo real hacia el navegador. Cada cliente se suscribe a un lote
/// específico (por su Id) uniéndose a un "grupo" de SignalR; así el servidor puede
/// avisarle SOLO a quien subió ese lote cuándo terminó, sin que un cliente vea el
/// tráfico de otro. SignalR ya viene incluido en el framework compartido de
/// ASP.NET Core: no requiere ningún paquete NuGet adicional en el lado del servidor.
/// </summary>
public sealed class ProcesamientoHub : Hub
{
    public async Task SuscribirseALote(string loteId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDeLote(loteId));

    public async Task DesuscribirseDeLote(string loteId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoDeLote(loteId));

    public static string GrupoDeLote(string loteId) => $"lote-{loteId}";
    public static string GrupoDeLote(Guid loteId) => GrupoDeLote(loteId.ToString());
}