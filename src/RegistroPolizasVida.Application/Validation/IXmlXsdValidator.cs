using RegistroPolizasVida.Domain.Common;

namespace RegistroPolizasVida.Application.Validation;

public interface IXmlXsdValidator
{
    ResultadoValidacion Validar(byte[] contenidoXml, byte[] contenidoXsd);
}