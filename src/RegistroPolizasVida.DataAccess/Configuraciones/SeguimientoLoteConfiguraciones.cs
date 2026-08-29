using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.DataAccess.Configuraciones;

public sealed class LoteCargaConfiguracion : IEntityTypeConfiguration<LoteCarga>
{
    public void Configure(EntityTypeBuilder<LoteCarga> builder)
    {
        builder.ToTable("LotesCarga");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.NombreArchivoZip).HasMaxLength(260).IsRequired();
        builder.Property(l => l.Estado).HasConversion<string>().HasMaxLength(30);

        builder.HasMany(l => l.Archivos)
            .WithOne(a => a.LoteCarga)
            .HasForeignKey(a => a.LoteCargaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.FechaCarga);

        // El cálculo de duración no es una columna: se ignora para el mapeo.
        builder.Ignore(l => l.DuracionSegundos);
    }
}

public sealed class ArchivoLoteConfiguracion : IEntityTypeConfiguration<ArchivoLote>
{
    public void Configure(EntityTypeBuilder<ArchivoLote> builder)
    {
        builder.ToTable("ArchivosLote");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.NombreArchivo).HasMaxLength(260).IsRequired();
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(30);

        builder.HasMany(a => a.Errores)
            .WithOne(e => e.ArchivoLote)
            .HasForeignKey(e => e.ArchivoLoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ErrorProcesamientoConfiguracion : IEntityTypeConfiguration<ErrorProcesamiento>
{
    public void Configure(EntityTypeBuilder<ErrorProcesamiento> builder)
    {
        builder.ToTable("ErroresProcesamiento");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.NumeroPoliza).HasMaxLength(20);
        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Mensaje).HasMaxLength(2000).IsRequired();
    }
}