using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RegistroPolizasVida.Domain.Entities;

namespace RegistroPolizasVida.DataAccess.Configuraciones;

public sealed class PolizaConfiguracion : IEntityTypeConfiguration<Poliza>
{
    public void Configure(EntityTypeBuilder<Poliza> builder)
    {
        builder.ToTable("Polizas");

        // NumeroPoliza es la clave natural: así "GuardarAsync" puede hacer upsert
        // simplemente buscando por este valor (no hace falta un Id sustituto).
        builder.HasKey(p => p.NumeroPoliza);
        builder.Property(p => p.NumeroPoliza).HasMaxLength(20);

        builder.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Moneda).HasMaxLength(3);
        builder.Property(p => p.MontoCobertura).HasColumnType("decimal(18,2)");

        // El tomador vive dentro de la fila de la póliza (relación 1 a 1 sin
        // identidad propia): se modela como "owned type", no como tabla aparte.
        builder.OwnsOne(p => p.Tomador, tomador =>
        {
            tomador.Property(t => t.TipoPersona).HasConversion<string>().HasMaxLength(20).HasColumnName("Tomador_TipoPersona");
            tomador.Property(t => t.Cedula).HasMaxLength(12).HasColumnName("Tomador_Cedula");
            tomador.Property(t => t.CedulaJuridica).HasMaxLength(12).HasColumnName("Tomador_CedulaJuridica");
            tomador.Property(t => t.Nombre).HasMaxLength(200).IsRequired().HasColumnName("Tomador_Nombre");
            tomador.Property(t => t.PrimerApellido).HasMaxLength(100).HasColumnName("Tomador_PrimerApellido");
            tomador.Property(t => t.SegundoApellido).HasMaxLength(100).HasColumnName("Tomador_SegundoApellido");
            tomador.Property(t => t.RazonSocial).HasMaxLength(250).HasColumnName("Tomador_RazonSocial");
            tomador.Property(t => t.Telefono).HasMaxLength(30).HasColumnName("Tomador_Telefono");
            tomador.Property(t => t.Correo).HasMaxLength(150).HasColumnName("Tomador_Correo");
            tomador.Property(t => t.Direccion).HasMaxLength(300).HasColumnName("Tomador_Direccion");
        });
        builder.Navigation(p => p.Tomador).IsRequired();

        builder.HasMany(p => p.Asegurados)
            .WithOne(a => a.Poliza)
            .HasForeignKey(a => a.PolizaNumero)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.LoteCargaOrigenId);
        builder.HasIndex(p => p.LoteCargaUltimaActualizacionId);
    }
}