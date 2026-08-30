using Microsoft.EntityFrameworkCore;
using RegistroPolizasVida.DataAccess.Configuraciones;
using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.DataAccess;

public sealed class RegistroPolizasVidaDbContext : DbContext
{
    public RegistroPolizasVidaDbContext(DbContextOptions<RegistroPolizasVidaDbContext> options) : base(options) { }

    public DbSet<Poliza> Polizas => Set<Poliza>();
    public DbSet<Asegurado> Asegurados => Set<Asegurado>();
    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();
    public DbSet<LoteCarga> Lotes => Set<LoteCarga>();
    public DbSet<ArchivoLote> ArchivosLote => Set<ArchivoLote>();
    public DbSet<ErrorProcesamiento> ErroresProcesamiento => Set<ErrorProcesamiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PolizaConfiguracion());
        modelBuilder.ApplyConfiguration(new AseguradoConfiguracion());
        modelBuilder.ApplyConfiguration(new BeneficiarioConfiguracion());
        modelBuilder.ApplyConfiguration(new LoteCargaConfiguracion());
        modelBuilder.ApplyConfiguration(new ArchivoLoteConfiguracion());
        modelBuilder.ApplyConfiguration(new ErrorProcesamientoConfiguracion());
    }
}