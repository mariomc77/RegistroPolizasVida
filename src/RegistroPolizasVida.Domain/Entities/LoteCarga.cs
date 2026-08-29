using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Representa una carga por lotes: un archivo .ZIP subido por el usuario, que puede
/// contener uno o más archivos .XML, cada uno con una o más pólizas. Esta entidad
/// es el objeto de seguimiento que el cliente consulta (por sondeo o por SignalR)
/// para saber en qué va el procesamiento en segundo plano.
/// </summary>
public class LoteCarga
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NombreArchivoZip { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    public DateTime? FechaInicioProcesamiento { get; set; }
    public DateTime? FechaFinProcesamiento { get; set; }

    public EstadoLote Estado { get; set; } = EstadoLote.Pendiente;

    public int TotalArchivos { get; set; }
    public int ArchivosProcesados { get; set; }

    public int TotalPolizas { get; set; }
    public int PolizasInsertadas { get; set; }
    public int PolizasActualizadas { get; set; }
    public int PolizasConError { get; set; }

    public List<ArchivoLote> Archivos { get; set; } = new();

    public double? DuracionSegundos =>
        FechaInicioProcesamiento is null || FechaFinProcesamiento is null
            ? null
            : (FechaFinProcesamiento.Value - FechaInicioProcesamiento.Value).TotalSeconds;
}