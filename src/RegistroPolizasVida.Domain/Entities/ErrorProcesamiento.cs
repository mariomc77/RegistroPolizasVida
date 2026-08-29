using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Un error puntual detectado durante el procesamiento (de esquema XSD, de regla de
/// negocio o de persistencia), asociado al archivo .XML donde ocurrió y, cuando aplica,
/// al número de póliza involucrado. Se guarda para que el usuario pueda revisar y
/// corregir el archivo origen sin tener que adivinar qué falló.
/// </summary>
public class ErrorProcesamiento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ArchivoLoteId { get; set; }
    public ArchivoLote? ArchivoLote { get; set; }

    public string? NumeroPoliza { get; set; }
    public TipoError Tipo { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Línea del XML donde ocurrió el error, si el validador la pudo determinar.</summary>
    public int? Linea { get; set; }

    public DateTime FechaError { get; set; } = DateTime.UtcNow;
}