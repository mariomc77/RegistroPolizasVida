using RegistroPolizasVida.Application.Dtos;
using RegistroPolizasVida.Domain.Common;
using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.Application.Validation;

/// <summary>
/// Reglas de negocio del dominio de pólizas de vida que el XSD, por diseño, no
/// puede expresar (relaciones entre campos, sumas, cardinalidades condicionadas
/// al tipo de póliza). Se ejecutan después de que el XML ya pasó la validación
/// de esquema.
/// </summary>
public interface IPolizaBusinessRuleValidator
{
    /// <summary>Valida una póliza individual: cardinalidad de asegurados, fechas, montos, sumas de beneficios.</summary>
    ResultadoValidacion ValidarPoliza(Poliza poliza);

    /// <summary>Valida reglas que involucran a todas las pólizas de un mismo archivo (p. ej. números duplicados).</summary>
    ResultadoValidacion ValidarLote(LotePolizasXml lote);
}