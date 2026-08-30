using RegistroPolizasVida.Application.BackgroundQueue;

namespace RegistroPolizasVida.Web.BackgroundServices;

/// <summary>
/// El "otro proceso" del que habla el reto: un <see cref="BackgroundService"/> que
/// vive durante toda la vida de la aplicación, sacando trabajos de
/// <see cref="IBackgroundTaskQueue"/> uno tras otro (aquí; el paralelismo real
/// sucede DENTRO de cada trabajo, en <c>BatchProcessingService</c>, sobre los
/// archivos de un mismo lote) y ejecutándolos sin bloquear al hilo que atiende
/// peticiones HTTP. Es lo que permite que <c>POST /api/lotes</c> responda de
/// inmediato con 202 Accepted mientras el procesamiento sigue en segundo plano.
/// </summary>
public sealed class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _cola;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue cola, ILogger<QueuedHostedService> logger)
    {
        _cola = cola;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de procesamiento de lotes iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var trabajo = await _cola.DesencolarAsync(stoppingToken);
            try
            {
                await trabajo(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Un trabajo encolado terminó con una excepción no controlada.");
            }
        }

        _logger.LogInformation("Worker de procesamiento de lotes detenido.");
    }
}