using RegistroPolizasVida.Application.Dtos;

namespace RegistroPolizasVida.Application.Parsing;

/// <summary>
/// Convierte un XML de lote (que ya pasó la validación de esquema) en objetos de
/// dominio. Se asume XML válido según el XSD; si algo inesperado aparece de todas
/// formas, se lanza FormatException para que la capa orquestadora lo
/// reporte como un hallazgo más, sin tumbar el procesamiento del resto del lote.
/// </summary>
public interface IPolizaXmlParser
{
    LotePolizasXml Parsear(Stream contenidoXml);
}