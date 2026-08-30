using RegistroPolizasVida.Domain.Common;

namespace RegistroPolizasVida.Application.Validation;

/// <summary>
/// Valida el contenido de un archivo .XML de un lote contra el esquema oficial
/// (polizas.xsd) antes de intentar interpretarlo. Es la primera línea de defensa:
/// un archivo que no cumple la forma esperada ni siquiera se intenta parsear.
/// </summary>
public interface IXsdBatchValidator
{
    /// <summary>Valida el XML completo y devuelve TODOS los errores encontrados (no solo el primero).</summary>
    ResultadoValidacion Validar(Stream contenidoXml);
}