using Nova.Pedidos.Dominio.Comun;

namespace Nova.Pedidos.Dominio.Pedidos;

/// <summary>Un producto dentro de un pedido. Es hija del agregado: no tiene repositorio propio.</summary>
public sealed class LineaPedido
{
    public const int LargoMaximoProducto = 100;

    private LineaPedido()
    {
    }

    public string Producto { get; private set; } = "";

    public int Cantidad { get; private set; }

    public decimal PrecioUnitario { get; private set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    public static LineaPedido Crear(string? producto, int cantidad, decimal precioUnitario)
    {
        var nombre = producto?.Trim() ?? "";
        if (nombre.Length is 0 or > LargoMaximoProducto)
        {
            throw new ExcepcionDominio("pedido.producto-invalido", $"El producto es obligatorio y tiene hasta {LargoMaximoProducto} caracteres.");
        }

        if (cantidad <= 0)
        {
            throw new ExcepcionDominio("pedido.cantidad-invalida", "La cantidad tiene que ser mayor que cero.");
        }

        if (precioUnitario < 0 || decimal.Round(precioUnitario, 2) != precioUnitario)
        {
            throw new ExcepcionDominio("pedido.precio-invalido", "El precio unitario no puede ser negativo y lleva hasta 2 decimales.");
        }

        return new LineaPedido { Producto = nombre, Cantidad = cantidad, PrecioUnitario = precioUnitario };
    }
}
