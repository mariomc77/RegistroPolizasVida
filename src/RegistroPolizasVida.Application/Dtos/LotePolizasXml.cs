using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.Application.Dtos;

/// Summary:
/// Representación en memoria de UN archivo .XML ya parseado: el atributo raíz
/// (loteId/fechaLote del XML, informativo) y las pólizas que contiene. No es una
/// entidad persistente -- es el objeto intermedio entre "XML validado" y "pólizas
/// guardadas en base de datos".
///
public sealed class LotePolizasXml
{
    public string LoteIdXml { get; init; } = string.Empty;
    public DateOnly FechaLote { get; init; }
    public List<Poliza> Polizas { get; init; } = new();
}