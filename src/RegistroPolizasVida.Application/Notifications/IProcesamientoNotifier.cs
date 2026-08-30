using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.Application.Notifications;

/// <summary>
/// Avisa a quien esté interesado (típicamente el navegador que subió el lote) sobre
/// cambios de estado del procesamiento. La capa de aplicación depende solo de esta
/// abstracción; quien la implementa con SignalR vive en la capa Web, para no acoplar
/// la lógica de negocio a un mecanismo de transporte concreto.
/// </summary>
public interface IProcesamientoNotifier
{
    Task NotificarInicioAsync(LoteCarga lote, CancellationToken ct = default);

    Task NotificarProgresoAsync(LoteCarga lote, CancellationToken ct = default);

    Task NotificarFinalizacionAsync(LoteCarga lote, CancellationToken ct = default);
}

/// <summary>Implementación nula, útil para pruebas o para correr el pipeline sin capa web.</summary>
public sealed class NullProcesamientoNotifier : IProcesamientoNotifier
{
    public Task NotificarInicioAsync(LoteCarga lote, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotificarProgresoAsync(LoteCarga lote, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotificarFinalizacionAsync(LoteCarga lote, CancellationToken ct = default) => Task.CompletedTask;
}