using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public const string FiltroActivos = "ClientesActivos";

    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasQueryFilter(FiltroActivos, c => c.FechaBaja == null);
        builder.Ignore(c => c.EstaActivo);

        builder.Property(c => c.Nombre).HasMaxLength(100);
        builder.Property(c => c.Apellido).HasMaxLength(100);
        builder.Property(c => c.Provincia).HasMaxLength(100);
        builder.Property(c => c.Localidad).HasMaxLength(100);
        builder.Property(c => c.Ocupacion).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(254);

        builder.Property(c => c.Dni).HasMaxLength(8).IsUnicode(false);
        builder.Property(c => c.Telefono).HasMaxLength(20).IsUnicode(false);

        // Respaldo en la base ante altas simultáneas con el mismo DNI.
        builder.HasIndex(c => c.Dni).IsUnique();
    }
}
