using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nova.Pedidos.Adaptadores.Controladores;
using Nova.Pedidos.Aplicacion.Comun;
using Nova.Pedidos.Aplicacion.Pedidos;

namespace Nova.Pedidos.Pruebas.Integracion;

/// <summary>El módulo Pedidos dentro del servicio: HTTP → caso de uso → SQL Server (base _PRUEBAS).</summary>
public sealed class PedidosPruebas(FabricaApi fabrica)
{
    private const string Ruta = "/api/v1/pedidos";

    private static readonly object PedidoValido = new
    {
        lineas = new[]
        {
            new { producto = "Café molido 500 g", cantidad = 2, precioUnitario = 8.50m },
            new { producto = "Taza de cerámica", cantidad = 1, precioUnitario = 12.00m },
        },
    };

    private static CancellationToken Cancelacion => TestContext.Current.CancellationToken;

    // Cada fabrica.CrearToken(...) es un usuario distinto (un «sub» nuevo): sirve para probar «solo lo suyo».
    private HttpClient Cliente() => Con("cliente.prueba", ["cliente"], ["pedidos.leer", "pedidos.escribir"]);

    private HttpClient Operador() => Con("operador.prueba", ["operador"], ["pedidos.leer", "pedidos.escribir"]);

    private HttpClient Admin() => Con("admin.prueba", ["admin"], ["pedidos.leer", "pedidos.escribir"]);

    [Fact]
    public async Task Sin_token_responde_401()
    {
        var respuesta = await fabrica.CreateClient().GetAsync(Ruta, Cancelacion);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_pedidos_escribir_no_se_crea_403()
    {
        var soloLectura = Con("cliente.lector", ["cliente"], ["pedidos.leer"]);

        var respuesta = await soloLectura.PostAsJsonAsync(Ruta, PedidoValido, Cancelacion);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_pedidos_leer_no_se_lista_403()
    {
        var sinScopes = Con("cliente.sin-scopes", ["cliente"], []);

        var respuesta = await sinScopes.GetAsync(Ruta, Cancelacion);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_responde_201_con_su_ubicacion_y_el_total_lo_calcula_el_pedido()
    {
        var cliente = Cliente();
        var cuerpo = new { lineas = new[] { new { producto = "Cuaderno A5", cantidad = 3, precioUnitario = 4.25m } }, total = 1m, estado = "Entregado" };

        var respuesta = await cliente.PostAsJsonAsync(Ruta, cuerpo, Cancelacion);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await Leer<PedidoDto>(respuesta);
        Assert.Matches(@"^PED-\d{6,}$", creado.Numero);
        Assert.Equal(12.75m, creado.Total);
        Assert.Equal("Registrado", creado.Estado);
        Assert.Equal(12.75m, Assert.Single(creado.Lineas).Subtotal);
        Assert.EndsWith($"{Ruta}/{creado.Numero}", respuesta.Headers.Location!.ToString());

        var leido = await cliente.GetFromJsonAsync<PedidoDto>(respuesta.Headers.Location, Cancelacion);
        Assert.Equal(creado.Numero, leido!.Numero);
        Assert.Equal(creado.ClienteId, leido.ClienteId);
        Assert.Equal(12.75m, leido.Total);
        Assert.Equal("Cuaderno A5", Assert.Single(leido.Lineas).Producto);
    }

    [Fact]
    public async Task El_duenio_es_el_sub_del_token_aunque_el_cuerpo_diga_otro()
    {
        var cliente = Cliente();
        var quienSoy = await cliente.GetFromJsonAsync<QuienSoyRespuesta>("/api/v1/quien-soy", Cancelacion);
        var sub = Guid.Parse(quienSoy!.Id);
        var ajeno = Guid.NewGuid();
        var cuerpo = new { lineas = new[] { new { producto = "Café", cantidad = 1, precioUnitario = 1m } }, clienteId = ajeno };

        var respuesta = await cliente.PostAsJsonAsync(Ruta, cuerpo, Cancelacion);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await Leer<PedidoDto>(respuesta);
        Assert.Equal(sub, creado.ClienteId);
        var leido = await Operador().GetFromJsonAsync<PedidoDto>($"{Ruta}/{creado.Numero}", Cancelacion);
        Assert.Equal(sub, leido!.ClienteId);
        Assert.NotEqual(ajeno, leido.ClienteId);
    }

    [Fact]
    public async Task El_cliente_solo_ve_los_suyos_y_el_ajeno_responde_404()
    {
        var duena = Cliente();
        var otro = Cliente();
        var numero = await Crear(duena);

        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"{Ruta}/{numero}", Cancelacion)).StatusCode);
        var paginaDelOtro = await otro.GetFromJsonAsync<PaginaDto<PedidoDto>>(Ruta, Cancelacion);
        Assert.Equal(0, paginaDelOtro!.Total);
        var paginaDeLaDuena = await duena.GetFromJsonAsync<PaginaDto<PedidoDto>>(Ruta, Cancelacion);
        Assert.Equal(numero, Assert.Single(paginaDeLaDuena!.Elementos).Numero);
    }

    [Fact]
    public async Task Operador_y_admin_ven_los_de_todos()
    {
        var numero = await Crear(Cliente());

        Assert.Equal(HttpStatusCode.OK, (await Operador().GetAsync($"{Ruta}/{numero}", Cancelacion)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Admin().GetAsync($"{Ruta}/{numero}", Cancelacion)).StatusCode);
        var pagina = await Operador().GetFromJsonAsync<PaginaDto<PedidoDto>>($"{Ruta}?estado=registrado&tamano=50", Cancelacion);
        Assert.True(pagina!.Total >= 1);
        Assert.All(pagina.Elementos, p => Assert.Equal("Registrado", p.Estado));
    }

    [Fact]
    public async Task El_cliente_no_avanza_el_estado_403()
    {
        var cliente = Cliente();
        var numero = await Crear(cliente);

        var respuesta = await cliente.PostAsync($"{Ruta}/{numero}/avanzar", null, Cancelacion);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_operador_avanza_en_orden_y_entregado_ya_no_avanza_409()
    {
        var numero = await Crear(Cliente());
        var operador = Operador();

        foreach (var esperado in new[] { "Pagado", "Enviado", "Entregado" })
        {
            var avance = await operador.PostAsync($"{Ruta}/{numero}/avanzar", null, Cancelacion);
            Assert.Equal(HttpStatusCode.OK, avance.StatusCode);
            Assert.Equal(esperado, (await Leer<PedidoDto>(avance)).Estado);
        }

        var otraVez = await Admin().PostAsync($"{Ruta}/{numero}/avanzar", null, Cancelacion);
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
        Assert.Equal("pedido.no-avanza", await CodigoDe(otraVez));
    }

    [Theory]
    [InlineData("""{ "lineas": [] }""", "pedido.sin-lineas")]
    [InlineData("""{ }""", "pedido.sin-lineas")]
    [InlineData("""{ "lineas": [{ "producto": "", "cantidad": 1, "precioUnitario": 1 }] }""", "pedido.producto-invalido")]
    [InlineData("""{ "lineas": [{ "cantidad": 1, "precioUnitario": 1 }] }""", "pedido.producto-invalido")]
    [InlineData("""{ "lineas": [{ "producto": "Café", "cantidad": 0, "precioUnitario": 1 }] }""", "pedido.cantidad-invalida")]
    [InlineData("""{ "lineas": [{ "producto": "Café", "cantidad": 1, "precioUnitario": -1 }] }""", "pedido.precio-invalido")]
    [InlineData("""{ "lineas": [{ "producto": "Café", "cantidad": 1, "precioUnitario": 1.005 }] }""", "pedido.precio-invalido")]
    public async Task Cada_regla_rota_responde_400_con_su_codigo(string cuerpo, string codigo)
    {
        var respuesta = await Cliente().PostAsync(Ruta, new StringContent(cuerpo, MediaTypeHeaderValue.Parse("application/json")), Cancelacion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(codigo, await CodigoDe(respuesta));
    }

    [Fact]
    public async Task El_producto_de_101_caracteres_responde_400()
    {
        var cuerpo = new { lineas = new[] { new { producto = new string('x', 101), cantidad = 1, precioUnitario = 1m } } };

        var respuesta = await Cliente().PostAsJsonAsync(Ruta, cuerpo, Cancelacion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("pedido.producto-invalido", await CodigoDe(respuesta));
    }

    [Theory]
    [InlineData("?tamano=51")]
    [InlineData("?tamano=0")]
    [InlineData("?pagina=0")]
    [InlineData("?estado=Volando")]
    public async Task Un_filtro_invalido_responde_400(string filtro)
    {
        var respuesta = await Cliente().GetAsync(Ruta + filtro, Cancelacion);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("pedido.filtro-invalido", await CodigoDe(respuesta));
    }

    [Theory]
    [InlineData("PED-999999999")]
    [InlineData("no-es-un-numero")]
    public async Task Un_numero_que_no_existe_responde_404(string numero)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Operador().GetAsync($"{Ruta}/{numero}", Cancelacion)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Operador().PostAsync($"{Ruta}/{numero}/avanzar", null, Cancelacion)).StatusCode);
    }

    private HttpClient Con(string usuario, string[] roles, string[] scopes)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fabrica.CrearToken(usuario, roles, scopes));
        return cliente;
    }

    private static async Task<string> Crear(HttpClient cliente)
    {
        var respuesta = await cliente.PostAsJsonAsync(Ruta, PedidoValido, Cancelacion);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await Leer<PedidoDto>(respuesta)).Numero;
    }

    private static async Task<T> Leer<T>(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<T>(Cancelacion))!;

    private static async Task<string?> CodigoDe(HttpResponseMessage respuesta)
    {
        using var problema = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(Cancelacion));
        return problema.RootElement.TryGetProperty("codigo", out var codigo) ? codigo.GetString() : null;
    }
}
