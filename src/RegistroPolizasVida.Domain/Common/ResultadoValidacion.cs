using RegistroPolizasVida.Domain.Enums;

namespace RegistroPolizasVida.Domain.Common;

/// <summary>Un hallazgo de validación (de esquema o de negocio), independiente de dónde se originó.</summary>
public record HallazgoValidacion(TipoError Tipo, string Mensaje, string? NumeroPoliza = null, int? Linea = null);

/// <summary>
/// Resultado inmutable de validar algo (un archivo, una póliza): o es válido,
/// o trae la lista de hallazgos que explican por qué no lo es. Se usa en toda
/// la capa de aplicación para no depender de excepciones como control de flujo.
/// </summary>
public class ResultadoValidacion
{
    public bool EsValido => Hallazgos.Count == 0;
    public IReadOnlyList<HallazgoValidacion> Hallazgos { get; }

    private ResultadoValidacion(IReadOnlyList<HallazgoValidacion> hallazgos) => Hallazgos = hallazgos;

    public static ResultadoValidacion Valido() => new(Array.Empty<HallazgoValidacion>());

    public static ResultadoValidacion Invalido(IEnumerable<HallazgoValidacion> hallazgos) =>
        new(hallazgos.ToList());

    public static ResultadoValidacion Invalido(HallazgoValidacion hallazgo) =>
        new(new[] { hallazgo });

    public static ResultadoValidacion Combinar(params ResultadoValidacion[] resultados) =>
        new(resultados.SelectMany(r => r.Hallazgos).ToList());
}
