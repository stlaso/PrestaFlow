using PrestaFlow.Domain.Entities;

namespace PrestaFlow.Application.Dtos;

public sealed record ClienteResumen(int Id, string Nombre, string Apellido, string Dni)
{
    public static ClienteResumen Desde(Cliente cliente) =>
        new(cliente.Id, cliente.Nombre, cliente.Apellido, cliente.Dni);
}
