using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RegistroPolizasVida.DataAccess.Repositorios;
using RegistroPolizasVida.Domain.Interfaces;

namespace RegistroPolizasVida.DataAccess;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra el acceso a datos (SQL Server vía EF Core) usando la cadena de conexión "Default".</summary>
    public static IServiceCollection AgregarAccesoDatos(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Default' en la configuración.");

        var opciones = new DbContextOptionsBuilder<RegistroPolizasVidaDbContext>()
            .UseSqlServer(cadenaConexion)
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;

        // Se registra el DbContextOptions ya construido (no AddDbContext) para poder
        // exponer también la fábrica que crea contextos frescos bajo demanda, usada
        // durante el procesamiento paralelo de un lote.
        servicios.AddSingleton(opciones);
        servicios.AddScoped(sp => new RegistroPolizasVidaDbContext(sp.GetRequiredService<DbContextOptions<RegistroPolizasVidaDbContext>>()));
        servicios.AddScoped<IUnitOfWork, UnitOfWork>();
        servicios.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

        return servicios;
    }
}