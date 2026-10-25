using Nova.Pedidos.Dominio.Comun;
using Nova.Pedidos.Dominio.Pedidos;

namespace Nova.Pedidos.Pruebas.Unitarias.Pedidos;

/// <summary>Las reglas del agregado Pedido, sin base ni HTTP: una prueba por regla de la ficha.</summary>
public sealed class PedidoPruebas
{
    private static readonly Guid Cliente = Guid.NewGuid();
    private static readonly DateTimeOffset Ahora = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Un_pedido_valido_nace_registrado_y_calcula_su_total()
    {
        var pedido = Pedido.Registrar(NumeroPedido.Desde(123), Cliente, Ahora,
            [LineaPedido.Crear("Café molido 500 g", 2, 8.50m), LineaPedido.Crear("Taza de cerámica", 1, 12m)]);

        Assert.Equal("PED-000123", pedido.Numero.ToString());
        Assert.Equal(EstadoPedido.Registrado, pedido.Estado);
        Assert.Equal(29.00m, pedido.Total);
        Assert.Equal(2, pedido.Lineas.Count);
        Assert.True(pedido.EsDe(Cliente));
        Assert.False(pedido.EsDe(Guid.NewGuid()));
    }

    [Fact]
    public void Sin_lineas_no_hay_pedido()
    {
        Assert.Equal("pedido.sin-lineas", Codigo(() => Pedido.Registrar(NumeroPedido.Desde(1), Cliente, Ahora, [])));
        Assert.Equal("pedido.sin-lineas", Codigo(() => Pedido.Registrar(NumeroPedido.Desde(1), Cliente, Ahora, null)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void El_producto_es_obligatorio(string? producto) =>
        Assert.Equal("pedido.producto-invalido", Codigo(() => LineaPedido.Crear(producto, 1, 1m)));

    [Fact]
    public void El_producto_tiene_hasta_100_caracteres()
    {
        Assert.Equal("pedido.producto-invalido", Codigo(() => LineaPedido.Crear(new string('x', 101), 1, 1m)));
        Assert.Equal(100, LineaPedido.Crear(new string('x', 100), 1, 1m).Producto.Length);
        Assert.Equal("Café", LineaPedido.Crear("  Café  ", 1, 1m).Producto);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void La_cantidad_es_mayor_que_cero(int cantidad) =>
        Assert.Equal("pedido.cantidad-invalida", Codigo(() => LineaPedido.Crear("Café", cantidad, 1m)));

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.005")]
    public void El_precio_no_es_negativo_y_lleva_hasta_2_decimales(string precio) =>
        Assert.Equal("pedido.precio-invalido", Codigo(() => LineaPedido.Crear("Café", 1, decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture))));

    [Fact]
    public void Un_precio_de_cero_vale()
    {
        Assert.Equal(0m, LineaPedido.Crear("Muestra gratis", 1, 0m).Subtotal);
    }

    [Fact]
    public void El_estado_avanza_en_orden_hasta_entregado()
    {
        var pedido = Pedido.Registrar(NumeroPedido.Desde(1), Cliente, Ahora, [LineaPedido.Crear("Café", 1, 1m)]);

        pedido.Avanzar();
        Assert.Equal(EstadoPedido.Pagado, pedido.Estado);
        pedido.Avanzar();
        Assert.Equal(EstadoPedido.Enviado, pedido.Estado);
        pedido.Avanzar();
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
    }

    [Fact]
    public void Un_pedido_entregado_ya_no_avanza()
    {
        var pedido = Pedido.Registrar(NumeroPedido.Desde(1), Cliente, Ahora, [LineaPedido.Crear("Café", 1, 1m)]);
        pedido.Avanzar();
        pedido.Avanzar();
        pedido.Avanzar();

        Assert.Equal("pedido.no-avanza", Codigo(pedido.Avanzar));
        Assert.Equal(EstadoPedido.Entregado, pedido.Estado);
    }

    [Theory]
    [InlineData("PED-000123", 123)]
    [InlineData("ped-42", 42)]
    [InlineData("PED-1234567", 1234567)]
    public void El_numero_se_lee_desde_su_texto(string texto, long valor)
    {
        Assert.True(NumeroPedido.TryParse(texto, out var numero));
        Assert.Equal(valor, numero.Valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PED-")]
    [InlineData("PED-000000")]
    [InlineData("PED--1")]
    [InlineData("PED-12a")]
    [InlineData("123")]
    [InlineData("PED-99999999999999999999")]
    public void Lo_que_no_tiene_forma_de_numero_no_es_un_numero(string? texto) =>
        Assert.False(NumeroPedido.TryParse(texto, out _));

    private static string Codigo(Action accion) => Assert.Throws<ExcepcionDominio>(accion).Codigo;

    private static string Codigo(Func<object> accion) => Assert.Throws<ExcepcionDominio>(accion).Codigo;
}
