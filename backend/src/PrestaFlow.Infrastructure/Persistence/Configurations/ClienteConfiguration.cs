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

        builder.Property(c => c.Nombre).HasMaxLength(Cliente.LargoMaximoTexto);
        builder.Property(c => c.Apellido).HasMaxLength(Cliente.LargoMaximoTexto);
        builder.Property(c => c.Provincia).HasMaxLength(Cliente.LargoMaximoTexto);
        builder.Property(c => c.Localidad).HasMaxLength(Cliente.LargoMaximoTexto);
        builder.Property(c => c.Ocupacion).HasMaxLength(Cliente.LargoMaximoTexto);
        builder.Property(c => c.Email).HasMaxLength(Cliente.LargoMaximoEmail);

        builder.Property(c => c.Dni).HasMaxLength(Cliente.LargoMaximoDni).IsUnicode(false);
        builder.Property(c => c.Telefono).HasMaxLength(Cliente.LargoMaximoTelefono).IsUnicode(false);

        // Respaldo en la base ante altas simultáneas con el mismo DNI.
        builder.HasIndex(c => c.Dni).IsUnique();
    }
}
