using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Persona física o jurídica que adquiere la póliza. No necesariamente coincide
/// con el asegurado (ver documentación del dominio: Tomador vs Asegurado).
/// Se modela como "owned entity" de <see cref="Poliza"/> (1 a 1, sin tabla propia
/// con Id independiente) porque no tiene existencia fuera de la póliza que lo referencia.
/// </summary>
public class Tomador
{
    public TipoPersona TipoPersona { get; set; }

    // Persona física
    public string? Cedula { get; set; }
    public string? PrimerApellido { get; set; }
    public string? SegundoApellido { get; set; }

    // Persona jurídica
    public string? CedulaJuridica { get; set; }
    public string? RazonSocial { get; set; }

    // Comunes
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }

    public string NombreCompleto =>
        TipoPersona == TipoPersona.Juridica
            ? Nombre
            : string.Join(' ', new[] { Nombre, PrimerApellido, SegundoApellido }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Identificador único de la persona (cédula física o jurídica), útil para comparar identidad.</summary>
    public string IdentificacionUnica => TipoPersona == TipoPersona.Juridica
        ? (CedulaJuridica ?? string.Empty)
        : (Cedula ?? string.Empty);
}