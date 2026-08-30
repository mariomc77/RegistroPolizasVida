namespace RegistroPolizasVida.Application.BackgroundQueue;

/// <summary>
/// Cola productor/consumidor para desacoplar la petición HTTP (que solo debe encolar
/// el trabajo y responder de inmediato) del procesamiento pesado (que corre en un
/// <c>BackgroundService</c> aparte). Esto es lo que responde a la pregunta del reto:
/// "¿podría cargar un archivo y enviárselo a otro proceso para que lo procese... y
/// permitirle a mi programa continuar con otra tarea?" — el "otro proceso" es este
/// worker en segundo plano, y el aviso de que terminó llega por notificación
/// (ver <see cref="Notifications.IProcesamientoNotifier"/>), no bloqueando al cliente.
/// </summary>
public interface IBackgroundTaskQueue
{
    ValueTask EncolarAsync(Func<CancellationToken, Task> trabajo);

    ValueTask<Func<CancellationToken, Task>> DesencolarAsync(CancellationToken ct);
}