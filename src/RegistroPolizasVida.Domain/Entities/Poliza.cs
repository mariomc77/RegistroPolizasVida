using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Póliza de vida. Raíz de agregado: agrupa un tomador, uno o más asegurados y,
/// para cada asegurado, sus beneficiarios. <see cref="NumeroPoliza"/> es la clave
/// natural que identifica la póliza entre lotes (permite detectar actualizaciones:
/// si un lote nuevo trae un número de póliza ya existente, sus datos se reemplazan).
/// </summary>
public class Poliza
{
    /// <summary>Clave primaria natural (viene del XML, formato validado por el XSD).</summary>
    public string NumeroPoliza { get; set; } = string.Empty;

    public TipoPoliza Tipo { get; set; }
    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal MontoCobertura { get; set; }
    public string Moneda { get; set; } = "CRC";

    public Tomador Tomador { get; set; } = new();
    public List<Asegurado> Asegurados { get; set; } = new();

    // --- Metadatos de trazabilidad del procesamiento por lotes ---

    /// <summary>Id del lote (carga de .ZIP) que insertó la póliza por primera vez.</summary>
    public Guid LoteCargaOrigenId { get; set; }

    /// <summary>Id del último lote que modificó la póliza (igual al de origen si nunca se actualizó).</summary>
    public Guid LoteCargaUltimaActualizacionId { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    /// <summary>Cantidad de veces que la póliza ha sido reemplazada por un lote posterior.</summary>
    public int VersionActualizacion { get; set; } = 1;
}