using Microsoft.AspNetCore.Mvc;
using PrestaFlow.Application.Dtos;
using PrestaFlow.Application.Interfaces;

namespace PrestaFlow.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController(IClienteService clienteService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteResponse>> Crear(CrearClienteRequest request, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id }, cliente);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var cliente = await clienteService.ObtenerPorIdAsync(id, cancellationToken);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClienteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteResponse>>> Listar([FromQuery] bool incluirBajas = false, CancellationToken cancellationToken = default)
    {
        var clientes = await clienteService.ListarAsync(incluirBajas, cancellationToken);
        return Ok(clientes);
    }
}
