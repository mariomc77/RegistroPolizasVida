using RegistroPolizasVida.Application.Common;

namespace RegistroPolizasVida.Web.Logging;

/// <summary>
/// Adaptador delgado: implementa la abstracción de logging de la capa de
/// aplicación (<see cref="IPipelineLogger{T}"/>) delegando en el logging real de
/// ASP.NET Core (<c>ILogger&lt;T&gt;</c>), que trae salida estructurada, niveles
/// configurables por sección en appsettings.json, etc. Así la capa Application no
/// depende de Microsoft.Extensions.Logging directamente.
/// </summary>
public sealed class MicrosoftPipelineLogger<T> : IPipelineLogger<T>
{
    private readonly ILogger<T> _logger;
    public MicrosoftPipelineLogger(ILogger<T> logger) => _logger = logger;

    public void LogInformation(string mensaje, params object?[] args) => _logger.LogInformation(mensaje, args);
    public void LogWarning(string mensaje, params object?[] args) => _logger.LogWarning(mensaje, args);
    public void LogError(Exception? ex, string mensaje, params object?[] args) => _logger.LogError(ex, mensaje, args);
}