using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Interfaces;
using PrestaFlow.Domain.Entities;
using PrestaFlow.Domain.Exceptions;

namespace PrestaFlow.Application.Services;

public class ClienteService(IClienteRepository clientes, IUnitOfWork unitOfWork) : IClienteService
{
    public async Task<ClienteResponse> CrearAsync(CrearClienteRequest request, CancellationToken cancellationToken = default)
    {
        var cliente = new Cliente(
            nombre: request.Nombre,
            apellido: request.Apellido,
            dni: request.Dni,
            provincia: request.Provincia,
            localidad: request.Localidad,
            telefono: request.Telefono,
            email: request.Email,
            fechaNacimiento: request.FechaNacimiento,
            ocupacion: request.Ocupacion);

        // Se busca con el DNI ya normalizado por la entidad.
        var existente = await clientes.ObtenerPorDniIncluyendoBajasAsync(cliente.Dni, cancellationToken);

        if (existente is not null)
        {
            throw existente.EstaActivo
                ? new ConflictoException($"Ya existe un cliente con el DNI {cliente.Dni}.")
                : new ConflictoException($"Existe un cliente dado de baja con el DNI {cliente.Dni} (Id {existente.Id}). Reactivalo en lugar de darlo de alta.");
        }

        clientes.Agregar(cliente);
        await unitOfWork.GuardarCambiosAsync(cancellationToken);

        return ClienteResponse.Desde(cliente);
    }

    public async Task<ClienteResponse?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cliente = await clientes.ObtenerPorIdAsync(id, cancellationToken);

        return cliente is null ? null : ClienteResponse.Desde(cliente);
    }

    public async Task<IReadOnlyList<ClienteResponse>> ListarAsync(bool incluirBajas, CancellationToken cancellationToken = default)
    {
        var lista = await clientes.ListarAsync(incluirBajas, cancellationToken);

        return lista.Select(ClienteResponse.Desde).ToList();
    }
}
