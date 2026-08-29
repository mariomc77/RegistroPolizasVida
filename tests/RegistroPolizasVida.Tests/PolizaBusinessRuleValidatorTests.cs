using RegistroPolizasVida.Application.Validation;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Tests;

public class PolizaBusinessRuleValidatorTests
{
    private readonly PolizaBusinessRuleValidator _validador = new();

    private static Poliza PolizaIndividualValida() => new()
    {
        NumeroPoliza = "POL-TEST-0001",
        Tipo = TipoPoliza.Individual,
        FechaEmision = new DateOnly(2026, 1, 1),
        FechaVencimiento = new DateOnly(2030, 1, 1),
        MontoCobertura = 1000m,
        Moneda = "CRC",
        Tomador = new Tomador { TipoPersona = TipoPersona.Fisica, Cedula = "01-0000-0000", Nombre = "Ana" },
        Asegurados = new List<Asegurado>
        {
            new()
            {
                Cedula = "02-0000-0000",
                Nombre = "Beto",
                PrimerApellido = "Pérez",
                SegundoApellido = "Solano",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Beneficiarios = new List<Beneficiario>
                {
                    new() { TipoPersona = TipoPersona.Fisica, Cedula = "03-0000-0000", Nombre = "Carla", PorcentajeBeneficio = 60m },
                    new() { TipoPersona = TipoPersona.Fisica, Cedula = "04-0000-0000", Nombre = "Diego", PorcentajeBeneficio = 40m },
                }
            }
        }
    };

    [Fact]
    public void Una_poliza_individual_bien_formada_es_valida()
    {
        var resultado = _validador.ValidarPoliza(PolizaIndividualValida());
        Assert.True(resultado.EsValido);
    }

    [Fact]
    public void Individual_con_mas_de_un_asegurado_es_invalida()
    {
        var poliza = PolizaIndividualValida();
        poliza.Asegurados.Add(poliza.Asegurados[0]);

        var resultado = _validador.ValidarPoliza(poliza);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Hallazgos, h => h.Mensaje.Contains("Individual"));
    }

    [Fact]
    public void Colectiva_con_un_solo_asegurado_es_invalida()
    {
        var poliza = PolizaIndividualValida();
        poliza.Tipo = TipoPoliza.Colectiva;

        var resultado = _validador.ValidarPoliza(poliza);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Hallazgos, h => h.Mensaje.Contains("Colectiva"));
    }

    [Theory]
    [InlineData(99.99)]
    [InlineData(100.01)]
    public void Beneficiarios_que_no_suman_100_son_invalidos(decimal porcentajeSegundo)
    {
        var poliza = PolizaIndividualValida();
        // El primero queda en 60%; se ajusta el segundo para que la suma NO dé 100%.
        poliza.Asegurados[0].Beneficiarios[1].PorcentajeBeneficio = porcentajeSegundo;

        var resultado = _validador.ValidarPoliza(poliza);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Hallazgos, h => h.Mensaje.Contains("100.00%"));
    }

    [Fact]
    public void Fecha_de_vencimiento_anterior_a_emision_es_invalida()
    {
        var poliza = PolizaIndividualValida();
        poliza.FechaVencimiento = poliza.FechaEmision.AddDays(-1);

        var resultado = _validador.ValidarPoliza(poliza);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Hallazgos, h => h.Mensaje.Contains("vencimiento"));
    }

    [Fact]
    public void Monto_de_cobertura_cero_o_negativo_es_invalido()
    {
        var poliza = PolizaIndividualValida();
        poliza.MontoCobertura = 0m;

        var resultado = _validador.ValidarPoliza(poliza);

        Assert.False(resultado.EsValido);
    }

    [Fact]
    public void Un_asegurado_sin_beneficiarios_es_invalido()
    {
        var poliza = PolizaIndividualValida();
        poliza.Asegurados[0].Beneficiarios.Clear();

        var resultado = _validador.ValidarPoliza(poliza);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Hallazgos, h => h.Mensaje.Contains("no tiene beneficiarios"));
    }

    [Fact]
    public void Numeros_de_poliza_duplicados_en_el_mismo_archivo_se_detectan()
    {
        var lote = new RegistroPolizasVida.Application.Dtos.LotePolizasXml
        {
            LoteIdXml = "LOTE-TEST",
            FechaLote = new DateOnly(2026, 1, 1),
            Polizas = new List<Poliza> { PolizaIndividualValida(), PolizaIndividualValida() } // mismo NumeroPoliza
        };

        var resultado = _validador.ValidarLote(lote);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Hallazgos, h => h.Mensaje.Contains("más de una vez"));
    }
}