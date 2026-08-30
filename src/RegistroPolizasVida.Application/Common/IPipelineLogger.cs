namespace RegistroPolizasVida.Application.Common;

/// <summary>
/// Abstracción mínima de logging para no acoplar la capa de aplicación a un
/// framework de logging concreto. En la capa Web se implementa como un adaptador
/// delgado sobre Microsoft.Extensions.Logging.ILogger&lt;T&gt;; en la
/// herramienta de autochequeo de consola se implementa escribiendo a la consola.
/// </summary>
public interface IPipelineLogger<out T>
{
    void LogInformation(string mensaje, params object?[] args);
    void LogWarning(string mensaje, params object?[] args);
    void LogError(Exception? ex, string mensaje, params object?[] args);
}

/// <summary>Implementación de respaldo que escribe a la consola; útil fuera de un host web.</summary>
public sealed class ConsolePipelineLogger<T> : IPipelineLogger<T>
{
    public void LogInformation(string mensaje, params object?[] args) =>
        Console.WriteLine($"[INFO] {typeof(T).Name}: {Formatear(mensaje, args)}");

    public void LogWarning(string mensaje, params object?[] args) =>
        Console.WriteLine($"[WARN] {typeof(T).Name}: {Formatear(mensaje, args)}");

    public void LogError(Exception? ex, string mensaje, params object?[] args) =>
        Console.WriteLine($"[ERROR] {typeof(T).Name}: {Formatear(mensaje, args)}{(ex is null ? string.Empty : $" -> {ex}")}");

    /// <summary>
    /// Reemplaza tokens con nombre (p. ej. "{LoteId}") por los argumentos, en el
    /// orden en que aparecen -- el mismo estilo de plantilla semántica que usa
    /// Microsoft.Extensions.Logging, para que los call sites no dependan de cuál
    /// implementación de IPipelineLogger está activa.
    /// </summary>
    private static string Formatear(string mensaje, object?[] args)
    {
        if (args.Length == 0) return mensaje;
        var resultado = mensaje;
        var indiceArg = 0;
        var inicio = resultado.IndexOf('{');
        while (inicio >= 0 && indiceArg < args.Length)
        {
            var fin = resultado.IndexOf('}', inicio);
            if (fin < 0) break;
            var valor = args[indiceArg++]?.ToString() ?? "null";
            resultado = resultado[..inicio] + valor + resultado[(fin + 1)..];
            inicio = resultado.IndexOf('{', inicio + valor.Length);
        }
        return resultado;
    }
}