using System.Text;
using RegistroPolizasVida.Application.Validation;

namespace RegistroPolizasVida.Tests;

public class XsdBatchValidatorTests
{
    private readonly XsdBatchValidator _validador = new();

    private const string XmlValido = """
        <?xml version="1.0" encoding="UTF-8"?>
        <lotePolizas loteId="LOTE-TEST" fechaLote="2026-01-01">
          <poliza>
            <numeroPoliza>POL-TEST-0001</numeroPoliza>
            <tipo>Individual</tipo>
            <fechaEmision>2026-01-01</fechaEmision>
            <fechaVencimiento>2030-01-01</fechaVencimiento>
            <montoCobertura>1000.00</montoCobertura>
            <moneda>CRC</moneda>
            <tomador>
              <tipoPersona>Fisica</tipoPersona>
              <cedula>01-0000-0000</cedula>
              <nombre>Ana</nombre>
            </tomador>
            <asegurados>
              <asegurado>
                <cedula>02-0000-0000</cedula>
                <nombre>Beto</nombre>
                <primerApellido>Perez</primerApellido>
                <segundoApellido>Solano</segundoApellido>
                <fechaNacimiento>1990-01-01</fechaNacimiento>
                <beneficiarios>
                  <beneficiario>
                    <tipoPersona>Fisica</tipoPersona>
                    <cedula>03-0000-0000</cedula>
                    <nombre>Carla</nombre>
                    <porcentajeBeneficio>100.00</porcentajeBeneficio>
                  </beneficiario>
                </beneficiarios>
              </asegurado>
            </asegurados>
          </poliza>
        </lotePolizas>
        """;

    [Fact]
    public void Un_xml_bien_formado_y_conforme_al_xsd_es_valido()
    {
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes(XmlValido));
        var resultado = _validador.Validar(flujo);

        Assert.True(resultado.EsValido, string.Join("; ", resultado.Hallazgos.Select(h => h.Mensaje)));
    }

    [Fact]
    public void Numero_de_poliza_vacio_viola_minLength_del_xsd()
    {
        var xmlInvalido = XmlValido.Replace("<numeroPoliza>POL-TEST-0001</numeroPoliza>", "<numeroPoliza></numeroPoliza>");
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes(xmlInvalido));

        var resultado = _validador.Validar(flujo);

        Assert.False(resultado.EsValido);
    }

    [Fact]
    public void Tipo_de_poliza_fuera_de_la_enumeracion_es_invalido()
    {
        var xmlInvalido = XmlValido.Replace("<tipo>Individual</tipo>", "<tipo>Mixta</tipo>");
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes(xmlInvalido));

        var resultado = _validador.Validar(flujo);

        Assert.False(resultado.EsValido);
    }

    [Fact]
    public void Monto_de_cobertura_negativo_es_invalido()
    {
        var xmlInvalido = XmlValido.Replace("<montoCobertura>1000.00</montoCobertura>", "<montoCobertura>-1.00</montoCobertura>");
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes(xmlInvalido));

        var resultado = _validador.Validar(flujo);

        Assert.False(resultado.EsValido);
    }

    [Fact]
    public void Xml_mal_formado_no_lanza_excepcion_sino_que_reporta_el_hallazgo()
    {
        var xmlRoto = "<lotePolizas><poliza>";
        using var flujo = new MemoryStream(Encoding.UTF8.GetBytes(xmlRoto));

        var resultado = _validador.Validar(flujo);

        Assert.False(resultado.EsValido);
    }
}