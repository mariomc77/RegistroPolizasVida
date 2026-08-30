using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using RegistroPolizasVida.Domain.Common;
using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Application.Validation;

/// <summary>
/// Implementación basada en XmlSchemaSet / XmlReader.
/// Carga el esquema una sola vez (es inmutable y costoso de parsear) y lo reutiliza
/// para validar cada archivo del lote; por eso se registra como singleton en DI.
/// </summary>
public sealed class XsdBatchValidator : IXsdBatchValidator
{
    private const string RecursoEsquema = "RegistroPolizasVida.Application.Schemas.polizas.xsd";
    private readonly XmlSchemaSet _esquemas;

    public XsdBatchValidator()
    {
        _esquemas = new XmlSchemaSet();

        var ensamblado = Assembly.GetExecutingAssembly();
        using var flujoEsquema = ensamblado.GetManifestResourceStream(RecursoEsquema)
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso embebido '{RecursoEsquema}'. " +
                "Verifique que Schemas/polizas.xsd esté declarado como EmbeddedResource en el .csproj.");

        using var lector = XmlReader.Create(flujoEsquema);
        _esquemas.Add(targetNamespace: null, schemaDocument: lector);
        _esquemas.Compile();
    }

    public ResultadoValidacion Validar(Stream contenidoXml)
    {
        var hallazgos = new List<HallazgoValidacion>();

        var configuracion = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = _esquemas,
            // DTD deshabilitado explícitamente: los lotes solo deben traer datos, no
            // definiciones de tipo externas (mitiga ataques de entidades XML/XXE).
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        configuracion.ValidationEventHandler += (_, args) =>
        {
            hallazgos.Add(new HallazgoValidacion(
                Tipo: TipoError.EsquemaXsd,
                Mensaje: args.Message,
                Linea: args.Exception?.LineNumber));
        };

        try
        {
            using var lector = XmlReader.Create(contenidoXml, configuracion);
            while (lector.Read())
            {
                // Recorrer todo el documento es lo que dispara la validación de cada nodo.
            }
        }
        catch (XmlException ex)
        {
            // XML mal formado (ni siquiera es XML válido): se reporta como un hallazgo más,
            // en vez de dejar que la excepción se propague y tumbe el procesamiento del lote.
            hallazgos.Add(new HallazgoValidacion(TipoError.Formato, $"XML mal formado: {ex.Message}", Linea: ex.LineNumber));
        }

        return hallazgos.Count == 0
            ? ResultadoValidacion.Valido()
            : ResultadoValidacion.Invalido(hallazgos);
    }
}