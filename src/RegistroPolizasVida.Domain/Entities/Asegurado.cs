namespace RegistroPolizasVida.Domain.Entities;

/// <summary>
/// Persona física cuyo fallecimiento activa el cobro de la póliza. Una póliza
/// individual tiene exactamente un asegurado; una colectiva tiene dos o más
/// (ej. los empleados de una empresa tomadora).
/// </summary>
public class Asegurado
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Cedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string SegundoApellido { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    /// <summary>FK hacia la póliza a la que pertenece.</summary>
    public string PolizaNumero { get; set; } = string.Empty;
    public Poliza? Poliza { get; set; }

    public List<Beneficiario> Beneficiarios { get; set; } = new();

    public string NombreCompleto => $"{Nombre} {PrimerApellido} {SegundoApellido}".Trim();

    /// <summary>Suma de porcentajes asignados a los beneficiarios de este asegurado.</summary>
    public decimal SumaPorcentajesBeneficiarios => Beneficiarios.Sum(b => b.PorcentajeBeneficio);
}