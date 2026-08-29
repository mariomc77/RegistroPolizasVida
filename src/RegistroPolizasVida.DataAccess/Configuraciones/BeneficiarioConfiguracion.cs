using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.DataAccess.Configuraciones;

public sealed class BeneficiarioConfiguracion : IEntityTypeConfiguration<Beneficiario>
{
    public void Configure(EntityTypeBuilder<Beneficiario> builder)
    {
        builder.ToTable("Beneficiarios");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TipoPersona).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Cedula).HasMaxLength(12);
        builder.Property(b => b.CedulaJuridica).HasMaxLength(12);
        builder.Property(b => b.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(b => b.PrimerApellido).HasMaxLength(100);
        builder.Property(b => b.SegundoApellido).HasMaxLength(100);
        builder.Property(b => b.RazonSocial).HasMaxLength(250);
        builder.Property(b => b.PorcentajeBeneficio).HasColumnType("decimal(5,2)");
        builder.Property(b => b.Telefono).HasMaxLength(30);
        builder.Property(b => b.Correo).HasMaxLength(150);

        builder.HasIndex(b => b.AseguradoId);
    }
}