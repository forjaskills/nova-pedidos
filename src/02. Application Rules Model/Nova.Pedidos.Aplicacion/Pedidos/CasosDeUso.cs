using Nova.Pedidos.Aplicacion.Comun;
using Nova.Pedidos.Dominio.Comun;
using Nova.Pedidos.Dominio.Pedidos;

namespace Nova.Pedidos.Aplicacion.Pedidos;

// Casos de uso del módulo Pedidos: orquestan (leen, llaman al dominio, guardan) y devuelven Resultado<T>.
// Las reglas NO van aquí: van en la entidad. Una ExcepcionDominio se vuelve Error(codigo, mensaje, TipoError).

public sealed class ListarPedidos(IRepositorioPedidos repositorio) : IListarPedidos
{
    public const int TamanoMaximo = 50;

    public async Task<Resultado<PaginaDto<PedidoDto>>> Ejecutar(
        Solicitante solicitante, string? estado, int pagina, int tamano, CancellationToken cancelacion)
    {
        EstadoPedido? filtro = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.GetNames<EstadoPedido>().Contains(estado, StringComparer.OrdinalIgnoreCase))
            {
                return FiltroInvalido($"El estado «{estado}» no existe: {string.Join(", ", Enum.GetNames<EstadoPedido>())}.");
            }

            filtro = Enum.Parse<EstadoPedido>(estado, ignoreCase: true);
        }

        if (pagina < 1)
        {
            return FiltroInvalido("La página empieza en 1.");
        }

        if (tamano is < 1 or > TamanoMaximo)
        {
            return FiltroInvalido($"El tamaño de página va de 1 a {TamanoMaximo}.");
        }

        var (pedidos, total) = await repositorio.Listar(
            solicitante.GestionaTodos ? null : solicitante.Id, filtro, pagina, tamano, cancelacion);
        return new PaginaDto<PedidoDto>([.. pedidos.Select(PedidoDto.Desde)], total, pagina, tamano);
    }

    private static Error FiltroInvalido(string mensaje) => new("pedido.filtro-invalido", mensaje, TipoError.Validacion);
}

public sealed class ObtenerPedido(IRepositorioPedidos repositorio) : IObtenerPedido
{
    public async Task<Resultado<PedidoDto>> Ejecutar(Solicitante solicitante, string numero, CancellationToken cancelacion)
    {
        var pedido = await Buscar.Visible(repositorio, solicitante, numero, cancelacion);
        return pedido is null ? Buscar.NoEncontrado(numero) : PedidoDto.Desde(pedido);
    }
}

public sealed class CrearPedido(IRepositorioPedidos repositorio, TimeProvider reloj) : ICrearPedido
{
    public async Task<Resultado<PedidoDto>> Ejecutar(
        Solicitante solicitante, CrearPedidoSolicitud solicitud, CancellationToken cancelacion)
    {
        Pedido pedido;
        try
        {
            // Las líneas se validan antes de pedir el número: una línea inválida no gasta la secuencia.
            var lineas = solicitud.Lineas?.Select(l => LineaPedido.Crear(l.Producto, l.Cantidad, l.PrecioUnitario)).ToList();
            var numero = await repositorio.SiguienteNumero(cancelacion);
            pedido = Pedido.Registrar(numero, solicitante.Id, reloj.GetUtcNow(), lineas);
        }
        catch (ExcepcionDominio ex)
        {
            return new Error(ex.Codigo, ex.Message, TipoError.Validacion);
        }

        repositorio.Agregar(pedido);
        await repositorio.Guardar(cancelacion);
        return PedidoDto.Desde(pedido);
    }
}

public sealed class AvanzarPedido(IRepositorioPedidos repositorio) : IAvanzarPedido
{
    public async Task<Resultado<PedidoDto>> Ejecutar(Solicitante solicitante, string numero, CancellationToken cancelacion)
    {
        var pedido = await Buscar.Visible(repositorio, solicitante, numero, cancelacion);
        if (pedido is null)
        {
            return Buscar.NoEncontrado(numero);
        }

        try
        {
            pedido.Avanzar();
        }
        catch (ExcepcionDominio ex)
        {
            // La regla existe, pero el estado actual no la permite: es un conflicto (409), no un dato mal escrito.
            return new Error(ex.Codigo, ex.Message, TipoError.Conflicto);
        }

        await repositorio.Guardar(cancelacion);
        return PedidoDto.Desde(pedido);
    }
}

/// <summary>Lo que comparten Obtener y Avanzar: un número mal escrito, inexistente o ajeno es «no encontrado».</summary>
internal static class Buscar
{
    public static async Task<Pedido?> Visible(
        IRepositorioPedidos repositorio, Solicitante solicitante, string numero, CancellationToken cancelacion)
    {
        if (!NumeroPedido.TryParse(numero, out var valor))
        {
            return null;
        }

        var pedido = await repositorio.ObtenerPorNumero(valor, cancelacion);
        return pedido is not null && (solicitante.GestionaTodos || pedido.EsDe(solicitante.Id)) ? pedido : null;
    }

    public static Error NoEncontrado(string numero) =>
        new("pedido.no-encontrado", $"No existe el pedido {numero}.", TipoError.NoEncontrado);
}
