using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.DataAccess.Configuraciones;

public sealed class AseguradoConfiguracion : IEntityTypeConfiguration<Asegurado>
{
    public void Configure(EntityTypeBuilder<Asegurado> builder)
    {
        builder.ToTable("Asegurados");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Cedula).HasMaxLength(12).IsRequired();
        builder.Property(a => a.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PrimerApellido).HasMaxLength(100).IsRequired();
        builder.Property(a => a.SegundoApellido).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Telefono).HasMaxLength(30);
        builder.Property(a => a.Correo).HasMaxLength(150);
        builder.Property(a => a.PolizaNumero).HasMaxLength(20);

        builder.HasMany(a => a.Beneficiarios)
            .WithOne(b => b.Asegurado)
            .HasForeignKey(b => b.AseguradoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.Cedula);
        builder.HasIndex(a => a.PolizaNumero);
    }
}