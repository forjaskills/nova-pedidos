using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nova.Pedidos.Adaptadores.Presentadores;
using Nova.Pedidos.Aplicacion.Comun;
using Nova.Pedidos.Aplicacion.Pedidos;

namespace Nova.Pedidos.Adaptadores.Controladores;

/// <summary>
/// HTTP → casos de uso del módulo Pedidos. Aquí no hay reglas de negocio: solo se traduce la petición,
/// se arma el <see cref="Solicitante"/> con el token y se presenta el resultado (this.Presentar / this.Problema).
/// </summary>
[ApiController]
[Route("api/v1/pedidos")]
[Authorize(Policy = Politicas.Lectura)]
public sealed class ControladorPedidos : ControllerBase
{
    /// <summary>Una página de pedidos: el cliente ve los suyos; operador y admin, todos.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<PedidoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromServices] IListarPedidos listar,
        [FromQuery] string? estado,
        CancellationToken cancelacion,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20) =>
        this.Presentar(await listar.Ejecutar(Solicitante(), estado, pagina, tamano, cancelacion));

    /// <summary>Un pedido por su número (PED-000123).</summary>
    [HttpGet("{numero}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener([FromServices] IObtenerPedido obtener, string numero, CancellationToken cancelacion) =>
        this.Presentar(await obtener.Ejecutar(Solicitante(), numero, cancelacion));

    /// <summary>Registra un pedido a nombre de quien inicia sesión.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Escritura)]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(
        [FromServices] ICrearPedido crear, [FromBody] CrearPedidoSolicitud solicitud, CancellationToken cancelacion)
    {
        var resultado = await crear.Ejecutar(Solicitante(), solicitud, cancelacion);
        return resultado.EsExito
            ? CreatedAtAction(nameof(Obtener), new { numero = resultado.Valor!.Numero }, resultado.Valor)
            : this.Problema(resultado.Error!);
    }

    /// <summary>Pasa el pedido a su estado siguiente (operador y admin).</summary>
    [HttpPost("{numero}/avanzar")]
    [Authorize(Policy = Politicas.Escritura)]
    [Authorize(Policy = Politicas.Gestion)]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Avanzar([FromServices] IAvanzarPedido avanzar, string numero, CancellationToken cancelacion) =>
        this.Presentar(await avanzar.Ejecutar(Solicitante(), numero, cancelacion));

    private Solicitante Solicitante() =>
        new(Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty, GestionaTodos());

    private bool GestionaTodos() => User.HasClaim("role", "operador") || User.HasClaim("role", "admin");
}
