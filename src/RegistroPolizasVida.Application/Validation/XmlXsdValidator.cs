using System.Xml;
using System.Xml.Schema;
using RegistroPolizasVida.Domain.Common;
using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Application.Validation;

public sealed class XmlXsdValidator : IXmlXsdValidator
{
    public ResultadoValidacion Validar(byte[] contenidoXml, byte[] contenidoXsd)
    {
        var hallazgos = new List<HallazgoValidacion>();

        if (contenidoXml == null || contenidoXml.Length == 0)
        {
            hallazgos.Add(
                new HallazgoValidacion(
                    TipoError.Formato,
                    "El contenido XML está vacío."
                )
            );
        }

        if (contenidoXsd == null || contenidoXsd.Length == 0)
        {
            hallazgos.Add(
                new HallazgoValidacion(
                    TipoError.EsquemaXsd,
                    "El contenido XSD está vacío."
                )
            );
        }

        if (hallazgos.Count > 0)
        {
            return ResultadoValidacion.Invalido(hallazgos);
        }

        try
        {
            var esquemas = new XmlSchemaSet();

            using (var flujoXsd = new MemoryStream(contenidoXsd))
            using (var lectorXsd = XmlReader.Create(
                       flujoXsd,
                       new XmlReaderSettings
                       {
                           DtdProcessing = DtdProcessing.Prohibit,
                           XmlResolver = null
                       }))
            {
                esquemas.Add(null, lectorXsd);
                esquemas.Compile();
            }

            var configuracion = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = esquemas,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            configuracion.ValidationEventHandler += (_, args) =>
            {
                hallazgos.Add(
                    new HallazgoValidacion(
                        TipoError.EsquemaXsd,
                        args.Message,
                        Linea: args.Exception?.LineNumber
                    )
                );
            };

            using var flujoXml = new MemoryStream(contenidoXml);
            using var lectorXml = XmlReader.Create(flujoXml, configuracion);

            while (lectorXml.Read())
            {
            }

            return hallazgos.Count == 0
                ? ResultadoValidacion.Valido()
                : ResultadoValidacion.Invalido(hallazgos);
        }
        catch (XmlSchemaException ex)
        {
            hallazgos.Add(
                new HallazgoValidacion(
                    TipoError.EsquemaXsd,
                    $"XSD inválido: {ex.Message}",
                    Linea: ex.LineNumber
                )
            );

            return ResultadoValidacion.Invalido(hallazgos);
        }
        catch (XmlException ex)
        {
            hallazgos.Add(
                new HallazgoValidacion(
                    TipoError.Formato,
                    $"XML inválido: {ex.Message}",
                    Linea: ex.LineNumber
                )
            );

            return ResultadoValidacion.Invalido(hallazgos);
        }
        catch (Exception ex)
        {
            hallazgos.Add(
                new HallazgoValidacion(
                    TipoError.Formato,
                    $"Error durante la validación: {ex.Message}"
                )
            );

            return ResultadoValidacion.Invalido(hallazgos);
        }
    }
}