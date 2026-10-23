namespace PrestaFlow.Application.Dtos;

public sealed record CrearClienteRequest(
    string Nombre,
    string Apellido,
    string Dni,
    string Provincia,
    string Localidad,
    string Telefono,
    string Email,
    DateOnly FechaNacimiento,
    string Ocupacion);
