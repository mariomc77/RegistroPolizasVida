using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Persona física o jurídica designada por un <see cref="Asegurado"/> para recibir
/// la indemnización. Cada asegurado designa sus beneficiarios de forma confidencial
/// (independiente de otros asegurados dentro de una póliza colectiva).
/// </summary>
public class Beneficiario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TipoPersona TipoPersona { get; set; }

    public string? Cedula { get; set; }
    public string? PrimerApellido { get; set; }
    public string? SegundoApellido { get; set; }

    public string? CedulaJuridica { get; set; }
    public string? RazonSocial { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public decimal PorcentajeBeneficio { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    /// <summary>FK hacia el asegurado que lo designó.</summary>
    public Guid AseguradoId { get; set; }
    public Asegurado? Asegurado { get; set; }

    public string NombreCompleto =>
        TipoPersona == TipoPersona.Juridica
            ? Nombre
            : string.Join(' ', new[] { Nombre, PrimerApellido, SegundoApellido }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
}