using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RegistroPolizasVida.Application.Dtos;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Application.Parsing;

public sealed class PolizaXmlParser : IPolizaXmlParser
{
    public LotePolizasXml Parsear(Stream contenidoXml)
    {
        var documento = XDocument.Load(contenidoXml, LoadOptions.SetLineInfo);
        var raiz = documento.Root
            ?? throw new FormatException("El documento XML no tiene elemento raíz.");

        var lote = new LotePolizasXml
        {
            LoteIdXml = raiz.Attribute("loteId")?.Value ?? string.Empty,
            FechaLote = ParsearFecha(raiz.Attribute("fechaLote")?.Value, "fechaLote"),
            Polizas = raiz.Elements("poliza").Select(ParsearPoliza).ToList()
        };

        return lote;
    }

    private static Poliza ParsearPoliza(XElement el)
    {
        return new Poliza
        {
            NumeroPoliza = Requerido(el, "numeroPoliza"),
            Tipo = ParsearEnum<TipoPoliza>(Requerido(el, "tipo"), "tipo"),
            FechaEmision = ParsearFecha(Requerido(el, "fechaEmision"), "fechaEmision"),
            FechaVencimiento = ParsearFecha(Requerido(el, "fechaVencimiento"), "fechaVencimiento"),
            MontoCobertura = ParsearDecimal(Requerido(el, "montoCobertura"), "montoCobertura"),
            Moneda = Opcional(el, "moneda") ?? "CRC",
            Tomador = ParsearTomador(RequeridoElemento(el, "tomador")),
            Asegurados = RequeridoElemento(el, "asegurados").Elements("asegurado")
                .Select(ParsearAsegurado).ToList()
        };
    }

    private static Tomador ParsearTomador(XElement el) => new()
    {
        TipoPersona = ParsearEnum<TipoPersona>(Requerido(el, "tipoPersona"), "tipoPersona"),
        Cedula = Opcional(el, "cedula"),
        CedulaJuridica = Opcional(el, "cedulaJuridica"),
        Nombre = Requerido(el, "nombre"),
        PrimerApellido = Opcional(el, "primerApellido"),
        SegundoApellido = Opcional(el, "segundoApellido"),
        RazonSocial = Opcional(el, "razonSocial"),
        Telefono = Opcional(el, "telefono"),
        Correo = Opcional(el, "correo"),
        Direccion = Opcional(el, "direccion")
    };

    private static Asegurado ParsearAsegurado(XElement el) => new()
    {
        Cedula = Requerido(el, "cedula"),
        Nombre = Requerido(el, "nombre"),
        PrimerApellido = Requerido(el, "primerApellido"),
        SegundoApellido = Requerido(el, "segundoApellido"),
        FechaNacimiento = ParsearFecha(Requerido(el, "fechaNacimiento"), "fechaNacimiento"),
        Telefono = Opcional(el, "telefono"),
        Correo = Opcional(el, "correo"),
        Beneficiarios = RequeridoElemento(el, "beneficiarios").Elements("beneficiario")
            .Select(ParsearBeneficiario).ToList()
    };

    private static Beneficiario ParsearBeneficiario(XElement el) => new()
    {
        TipoPersona = ParsearEnum<TipoPersona>(Requerido(el, "tipoPersona"), "tipoPersona"),
        Cedula = Opcional(el, "cedula"),
        CedulaJuridica = Opcional(el, "cedulaJuridica"),
        Nombre = Requerido(el, "nombre"),
        PrimerApellido = Opcional(el, "primerApellido"),
        SegundoApellido = Opcional(el, "segundoApellido"),
        RazonSocial = Opcional(el, "razonSocial"),
        PorcentajeBeneficio = ParsearDecimal(Requerido(el, "porcentajeBeneficio"), "porcentajeBeneficio"),
        Telefono = Opcional(el, "telefono"),
        Correo = Opcional(el, "correo")
    };

    // --- Helpers de lectura, con mensajes de error que apuntan al elemento y línea exactos ---

    private static string Requerido(XElement padre, string nombreHijo)
    {
        var hijo = padre.Element(nombreHijo);
        if (hijo is null || string.IsNullOrWhiteSpace(hijo.Value))
        {
            var info = (IXmlLineInfo)padre;
            var linea = info.HasLineInfo() ? $" (línea {info.LineNumber})" : string.Empty;
            throw new FormatException($"Falta el elemento requerido <{nombreHijo}> dentro de <{padre.Name}>{linea}.");
        }
        return hijo.Value.Trim();
    }

    private static string? Opcional(XElement padre, string nombreHijo) =>
        padre.Element(nombreHijo)?.Value?.Trim();

    private static XElement RequeridoElemento(XElement padre, string nombreHijo) =>
        padre.Element(nombreHijo)
            ?? throw new FormatException($"Falta el elemento requerido <{nombreHijo}> dentro de <{padre.Name}>.");

    private static DateOnly ParsearFecha(string? valor, string campo)
    {
        if (!DateOnly.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            throw new FormatException($"El campo '{campo}' no tiene un formato de fecha válido: '{valor}'.");
        return fecha;
    }

    private static decimal ParsearDecimal(string valor, string campo)
    {
        if (!decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
            throw new FormatException($"El campo '{campo}' no tiene un formato numérico válido: '{valor}'.");
        return numero;
    }

    private static TEnum ParsearEnum<TEnum>(string valor, string campo) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(valor, ignoreCase: false, out var resultado))
            throw new FormatException($"El campo '{campo}' tiene un valor no reconocido: '{valor}'.");
        return resultado;
    }
}