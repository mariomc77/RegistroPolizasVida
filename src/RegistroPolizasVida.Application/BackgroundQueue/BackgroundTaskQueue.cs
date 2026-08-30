using System.Threading.Channels;

namespace RegistroPolizasVida.Application.BackgroundQueue;

/// <summary>
/// Implementación con <see cref="Channel{T}"/> (parte del BCL, sin dependencias externas):
/// un canal ilimitado en modo FIFO que puede tener múltiples productores (varias
/// cargas simultáneas) y un consumidor (el hosted service) leyendo en un ciclo async.
/// </summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _canal =
        Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public async ValueTask EncolarAsync(Func<CancellationToken, Task> trabajo)
    {
        ArgumentNullException.ThrowIfNull(trabajo);
        await _canal.Writer.WriteAsync(trabajo);
    }

    public async ValueTask<Func<CancellationToken, Task>> DesencolarAsync(CancellationToken ct) =>
        await _canal.Reader.ReadAsync(ct);
}