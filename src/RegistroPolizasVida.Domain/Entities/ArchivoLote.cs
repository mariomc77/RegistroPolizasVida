using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Estado de procesamiento de un archivo .XML individual dentro de un <see cref="LoteCarga"/>.
/// Cada archivo del .ZIP se procesa de forma independiente y en paralelo: que uno falle
/// (p. ej. no cumple el XSD) no impide que los demás se procesen correctamente.
/// </summary>
public class ArchivoLote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LoteCargaId { get; set; }
    public LoteCarga? LoteCarga { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;
    public EstadoArchivo Estado { get; set; } = EstadoArchivo.Pendiente;

    public int CantidadPolizas { get; set; }
    public int PolizasInsertadas { get; set; }
    public int PolizasActualizadas { get; set; }
    public int PolizasConError { get; set; }

    public List<ErrorProcesamiento> Errores { get; set; } = new();
}