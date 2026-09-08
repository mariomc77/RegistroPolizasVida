using System.Text;
using RegistroPolizasVida.Application.Validation;

namespace RegistroPolizasVida.Tests;

public class XmlXsdValidatorTests
{
    private readonly XmlXsdValidator _validador = new();

    private const string XsdValido = """
    <?xml version="1.0" encoding="UTF-8"?>
    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
      <xs:element name="persona">
        <xs:complexType>
          <xs:sequence>
            <xs:element name="nombre" type="xs:string"/>
            <xs:element name="edad" type="xs:int"/>
          </xs:sequence>
        </xs:complexType>
      </xs:element>
    </xs:schema>
    """;

    private const string XmlValido = """
    <?xml version="1.0" encoding="UTF-8"?>
    <persona>
      <nombre>Mario</nombre>
      <edad>21</edad>
    </persona>
    """;

    [Fact]
    public void Xml_valido_respecto_al_xsd_debe_ser_valido()
    {
        byte[] xml = Encoding.UTF8.GetBytes(XmlValido);
        byte[] xsd = Encoding.UTF8.GetBytes(XsdValido);

        var resultado = _validador.Validar(xml, xsd);

        Assert.True(resultado.EsValido);
        Assert.Empty(resultado.Hallazgos);
    }

    [Fact]
    public void Xml_invalido_respecto_al_xsd_debe_ser_invalido()
    {
        const string xmlInvalido = """
        <?xml version="1.0" encoding="UTF-8"?>
        <persona>
          <nombre>Mario</nombre>
          <edad>NO_ES_UN_NUMERO</edad>
        </persona>
        """;

        byte[] xml = Encoding.UTF8.GetBytes(xmlInvalido);
        byte[] xsd = Encoding.UTF8.GetBytes(XsdValido);

        var resultado = _validador.Validar(xml, xsd);

        Assert.False(resultado.EsValido);
        Assert.NotEmpty(resultado.Hallazgos);
    }

    [Fact]
    public void Xsd_invalido_o_corrupto_debe_retornar_error()
    {
        const string xsdCorrupto = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="persona">
        """;

        byte[] xml = Encoding.UTF8.GetBytes(XmlValido);
        byte[] xsd = Encoding.UTF8.GetBytes(xsdCorrupto);

        var resultado = _validador.Validar(xml, xsd);

        Assert.False(resultado.EsValido);
        Assert.NotEmpty(resultado.Hallazgos);
    }

    [Fact]
    public void Xml_invalido_o_corrupto_debe_retornar_error()
    {
        const string xmlCorrupto = """
        <persona>
          <nombre>Mario</nombre>
          <edad>21
        </persona>
        """;

        byte[] xml = Encoding.UTF8.GetBytes(xmlCorrupto);
        byte[] xsd = Encoding.UTF8.GetBytes(XsdValido);

        var resultado = _validador.Validar(xml, xsd);

        Assert.False(resultado.EsValido);
        Assert.NotEmpty(resultado.Hallazgos);
    }

    [Fact]
    public void Xml_vacio_debe_retornar_error()
    {
        byte[] xml = Array.Empty<byte>();
        byte[] xsd = Encoding.UTF8.GetBytes(XsdValido);

        var resultado = _validador.Validar(xml, xsd);

        Assert.False(resultado.EsValido);
        Assert.NotEmpty(resultado.Hallazgos);
    }

    [Fact]
    public void Xsd_vacio_debe_retornar_error()
    {
        byte[] xml = Encoding.UTF8.GetBytes(XmlValido);
        byte[] xsd = Array.Empty<byte>();

        var resultado = _validador.Validar(xml, xsd);

        Assert.False(resultado.EsValido);
        Assert.NotEmpty(resultado.Hallazgos);
    }

    [Fact]
    public void Manejo_de_errores_no_debe_lanzar_excepcion()
    {
        const string xmlInvalido = """
        <persona>
          <nombre>Mario</nombre>
          <edad>texto</edad>
        </persona>
        """;

        byte[] xml = Encoding.UTF8.GetBytes(xmlInvalido);
        byte[] xsd = Encoding.UTF8.GetBytes(XsdValido);

        var excepcion = Record.Exception(() =>
        {
            var resultado = _validador.Validar(xml, xsd);

            Assert.False(resultado.EsValido);
            Assert.NotEmpty(resultado.Hallazgos);
        });

        Assert.Null(excepcion);
    }
}