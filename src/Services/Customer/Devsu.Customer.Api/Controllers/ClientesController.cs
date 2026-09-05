using Devsu.Customer.Application.Clientes;
using Devsu.Customer.Application.Clientes.Dtos;
using Devsu.Customer.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Devsu.Customer.Api.Controllers;


[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public sealed class ClientesController : ControllerBase
{
    private readonly IClienteService _servicio;

    public ClientesController(IClienteService servicio) => _servicio = servicio;

   
    [HttpGet]
    [ProducesResponseType<PagedResult<ClienteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClienteResponse>>> Listar(
        [FromQuery] ClienteQuery query,
        CancellationToken ct)
        => Ok(await _servicio.ListarAsync(query, ct));

    [HttpGet("{clienteId}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> Obtener(string clienteId, CancellationToken ct)
        => Ok(await _servicio.ObtenerAsync(clienteId, ct));


    [HttpPost]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteResponse>> Crear(
        [FromBody] CrearClienteRequest request,
        CancellationToken ct)
    {
        var creado = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { clienteId = creado.ClienteId }, creado);
    }

    [HttpPut("{clienteId}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> Actualizar(
        string clienteId,
        [FromBody] ActualizarClienteRequest request,
        CancellationToken ct)
        => Ok(await _servicio.ActualizarAsync(clienteId, request, ct));

    
    [HttpPatch("{clienteId}")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteResponse>> ActualizarParcial(
        string clienteId,
        [FromBody] ActualizarParcialClienteRequest request,
        CancellationToken ct)
        => Ok(await _servicio.ActualizarParcialAsync(clienteId, request, ct));

  
    [HttpDelete("{clienteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(string clienteId, CancellationToken ct)
    {
        await _servicio.DesactivarAsync(clienteId, ct);

        return NoContent();
    }

    [HttpPatch("{clienteId}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CambiarPassword(
        string clienteId,
        [FromBody] CambiarPasswordRequest request,
        CancellationToken ct)
    {
        await _servicio.CambiarPasswordAsync(clienteId, request, ct);

        return NoContent();
    }
}
