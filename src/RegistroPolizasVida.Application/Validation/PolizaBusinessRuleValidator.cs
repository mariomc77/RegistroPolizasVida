using RegistroPolizasVida.Application.Dtos;
using RegistroPolizasVida.Domain.Common;
using RegistroPolizasVida.Domain.Entities;
using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Application.Validation;

public sealed class PolizaBusinessRuleValidator : IPolizaBusinessRuleValidator
{
    public ResultadoValidacion ValidarPoliza(Poliza poliza)
    {
        var hallazgos = new List<HallazgoValidacion>();
        void Fallo(string mensaje) =>
            hallazgos.Add(new HallazgoValidacion(TipoError.ReglaNegocio, mensaje, poliza.NumeroPoliza));

        // --- Cardinalidad de asegurados según el tipo de póliza ---
        switch (poliza.Tipo)
        {
            case TipoPoliza.Individual when poliza.Asegurados.Count != 1:
                Fallo($"Una póliza Individual debe tener exactamente 1 asegurado; tiene {poliza.Asegurados.Count}.");
                break;
            case TipoPoliza.Colectiva when poliza.Asegurados.Count < 2:
                Fallo($"Una póliza Colectiva debe tener 2 o más asegurados; tiene {poliza.Asegurados.Count}.");
                break;
        }

        // --- Fechas coherentes ---
        if (poliza.FechaVencimiento <= poliza.FechaEmision)
            Fallo($"La fecha de vencimiento ({poliza.FechaVencimiento:yyyy-MM-dd}) debe ser posterior a la de emisión ({poliza.FechaEmision:yyyy-MM-dd}).");

        // --- Monto de cobertura ---
        if (poliza.MontoCobertura <= 0)
            Fallo("El monto de cobertura debe ser mayor que cero.");

        // --- Identificación del tomador coherente con su tipo de persona ---
        if (poliza.Tomador.TipoPersona == TipoPersona.Fisica && string.IsNullOrWhiteSpace(poliza.Tomador.Cedula))
            Fallo("El tomador es persona física pero no trae cédula.");
        if (poliza.Tomador.TipoPersona == TipoPersona.Juridica && string.IsNullOrWhiteSpace(poliza.Tomador.CedulaJuridica))
            Fallo("El tomador es persona jurídica pero no trae cédula jurídica.");

        // --- Por cada asegurado: al menos un beneficiario y la suma de porcentajes = 100% ---
        foreach (var asegurado in poliza.Asegurados)
        {
            if (asegurado.Beneficiarios.Count == 0)
            {
                Fallo($"El asegurado {asegurado.Cedula} ({asegurado.NombreCompleto}) no tiene beneficiarios.");
                continue;
            }

            var suma = asegurado.SumaPorcentajesBeneficiarios;
            if (suma != 100.00m)
            {
                Fallo($"Los beneficiarios del asegurado {asegurado.Cedula} ({asegurado.NombreCompleto}) " +
                      $"suman {suma:0.00}% en vez de 100.00%.");
            }

            // Cada beneficiario físico o jurídico debe traer su identificación.
            foreach (var beneficiario in asegurado.Beneficiarios)
            {
                if (beneficiario.TipoPersona == TipoPersona.Fisica && string.IsNullOrWhiteSpace(beneficiario.Cedula))
                    Fallo($"Un beneficiario de {asegurado.Cedula} es persona física pero no trae cédula.");
                if (beneficiario.TipoPersona == TipoPersona.Juridica && string.IsNullOrWhiteSpace(beneficiario.CedulaJuridica))
                    Fallo($"Un beneficiario de {asegurado.Cedula} es persona jurídica pero no trae cédula jurídica.");
            }
        }

        // --- Asegurados duplicados dentro de la misma póliza ---
        var aseguradosDuplicados = poliza.Asegurados
            .GroupBy(a => a.Cedula)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var cedula in aseguradosDuplicados)
            Fallo($"La cédula {cedula} aparece más de una vez como asegurado en la misma póliza.");

        return hallazgos.Count == 0 ? ResultadoValidacion.Valido() : ResultadoValidacion.Invalido(hallazgos);
    }

    public ResultadoValidacion ValidarLote(LotePolizasXml lote)
    {
        var duplicadas = lote.Polizas
            .GroupBy(p => p.NumeroPoliza)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        var hallazgos = duplicadas
            .Select(numero => new HallazgoValidacion(
                TipoError.ReglaNegocio,
                $"El número de póliza '{numero}' aparece más de una vez dentro del mismo archivo.",
                numero))
            .ToList();

        return hallazgos.Count == 0 ? ResultadoValidacion.Valido() : ResultadoValidacion.Invalido(hallazgos);
    }
}