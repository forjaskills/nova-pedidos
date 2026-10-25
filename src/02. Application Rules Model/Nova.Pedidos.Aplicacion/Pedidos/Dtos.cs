using Nova.Pedidos.Dominio.Pedidos;

namespace Nova.Pedidos.Aplicacion.Pedidos;

// Lo que entra (…Solicitud) y lo que sale (…Dto) del módulo Pedidos. Nunca se devuelve la entidad.
// Solicitante y PaginaDto<T> ya están en Aplicacion/Comun (los comparten todos los módulos).

/// <summary>Lo que manda el cliente para crear un pedido: solo las líneas (el dueño, el total y el estado no).</summary>
/// <remarks>Todo es anulable a propósito: lo que falte lo rechaza el dominio con su código, no el model binding.</remarks>
public sealed record CrearPedidoSolicitud(IReadOnlyList<LineaPedidoSolicitud>? Lineas);

public sealed record LineaPedidoSolicitud(string? Producto, int Cantidad, decimal PrecioUnitario);

public sealed record PedidoDto(
    string Numero,
    Guid ClienteId,
    DateTimeOffset Fecha,
    string Estado,
    decimal Total,
    IReadOnlyList<LineaPedidoDto> Lineas)
{
    internal static PedidoDto Desde(Pedido pedido) => new(
        pedido.Numero.ToString(),
        pedido.ClienteId,
        pedido.Fecha,
        pedido.Estado.ToString(),
        pedido.Total,
        [.. pedido.Lineas.Select(l => new LineaPedidoDto(l.Producto, l.Cantidad, l.PrecioUnitario, l.Subtotal))]);
}

public sealed record LineaPedidoDto(string Producto, int Cantidad, decimal PrecioUnitario, decimal Subtotal);
