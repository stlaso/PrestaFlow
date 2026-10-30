using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Application.Dtos;

public sealed record ClienteResponse(
    int Id,
    string Nombre,
    string Apellido,
    string Dni,
    string Provincia,
    string Localidad,
    string Telefono,
    string Email,
    DateOnly FechaNacimiento,
    string Ocupacion,
    bool Activo,
    DateTime? FechaBaja)
{
    public static ClienteResponse Desde(Cliente cliente) => new(
        Id: cliente.Id,
        Nombre: cliente.Nombre,
        Apellido: cliente.Apellido,
        Dni: cliente.Dni,
        Provincia: cliente.Provincia,
        Localidad: cliente.Localidad,
        Telefono: cliente.Telefono,
        Email: cliente.Email,
        FechaNacimiento: cliente.FechaNacimiento,
        Ocupacion: cliente.Ocupacion,
        Activo: cliente.EstaActivo,
        FechaBaja: cliente.FechaBaja);
}
