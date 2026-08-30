using Microsoft.AspNetCore.Mvc;
using RegistroPolizasVida.Application.BackgroundQueue;
using RegistroPolizasVida.Application.Services;

namespace RegistroPolizasVida.Web.Controllers;

/// <summary>
/// API para subir lotes (.ZIP) y consultar su estado. El endpoint de carga es
/// deliberadamente "delgado": valida lo mínimo indispensable, persiste el registro
/// de seguimiento, ENCOLA el trabajo pesado y responde 202 de inmediato -- nunca
/// procesa el .ZIP en el propio hilo de la petición.
/// </summary>
[ApiController]
[Route("api/lotes")]
public sealed class LotesController : ControllerBase
{
    private const long TamanoMaximoBytes = 200L * 1024 * 1024; // 200 MB, ajustable según necesidad.

    private readonly ILoteCargaAppService _lotesApp;
    private readonly IBackgroundTaskQueue _cola;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LotesController> _logger;

    public LotesController(
        ILoteCargaAppService lotesApp,
        IBackgroundTaskQueue cola,
        IServiceScopeFactory scopeFactory,
        ILogger<LotesController> logger)
    {
        _lotesApp = lotesApp;
        _cola = cola;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Sube un lote (.ZIP con uno o más .XML) y encola su procesamiento.</summary>
    [HttpPost]
    [RequestSizeLimit(TamanoMaximoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = TamanoMaximoBytes)]
    public async Task<IActionResult> Cargar(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Debe adjuntar un archivo .ZIP con el campo 'archivo'." });

        if (!Path.GetExtension(archivo.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { mensaje = "El archivo debe tener extensión .zip." });

        byte[] contenido;
        using (var memoria = new MemoryStream())
        {
            await archivo.CopyToAsync(memoria, ct);
            contenido = memoria.ToArray();
        }

        var lote = await _lotesApp.CrearLoteAsync(archivo.FileName, ct);

        // Se encola una función que, al ejecutarse en el worker de fondo, abre su
        // PROPIO scope de DI (su propio IBatchProcessingService / DbContext) -- no
        // se capturan aquí servicios con ciclo de vida "scoped" de esta petición,
        // porque para cuando el worker la ejecute, esta petición ya habrá terminado.
        await _cola.EncolarAsync(async token =>
        {
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IBatchProcessingService>();
            await servicio.ProcesarLoteAsync(lote.Id, contenido, token);
        });

        _logger.LogInformation("Lote {LoteId} ({Archivo}) encolado para procesamiento.", lote.Id, archivo.FileName);

        return AcceptedAtAction(nameof(Obtener), new { id = lote.Id }, new
        {
            id = lote.Id,
            nombreArchivoZip = lote.NombreArchivoZip,
            estado = lote.Estado.ToString(),
            mensaje = "El lote fue recibido y se está procesando en segundo plano. " +
                      "Consulte GET /api/lotes/{id} o suscríbase por SignalR (hub /hubs/procesamiento) para conocer el resultado."
        });
    }

    /// <summary>Consulta el estado (y, si ya terminó, el detalle) de un lote.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var lote = await _lotesApp.ObtenerEstadoAsync(id, ct);
        return lote is null ? NotFound() : Ok(lote);
    }

    /// <summary>Lista los lotes más recientes (para el tablero principal).</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int cantidad, CancellationToken ct) =>
        Ok(await _lotesApp.ObtenerRecientesAsync(cantidad <= 0 ? 20 : cantidad, ct));
}