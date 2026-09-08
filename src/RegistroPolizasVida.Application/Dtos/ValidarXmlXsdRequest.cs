namespace RegistroPolizasVida.Application.Dtos;

public sealed class ValidarXmlXsdRequest
{
    public byte[] Xml { get; set; } = Array.Empty<byte>();

    public byte[] Xsd { get; set; } = Array.Empty<byte>();
}