using Nova.Pedidos.Aplicacion.Comun;

namespace Nova.Pedidos.Aplicacion.Pedidos;

// Puertos de entrada: lo que el mundo de afuera (un controlador, una prueba) le puede pedir al módulo Pedidos.

/// <summary>Una página de pedidos: el cliente, los suyos; operador y admin, los de todos.</summary>
public interface IListarPedidos
{
    Task<Resultado<PaginaDto<PedidoDto>>> Ejecutar(
        Solicitante solicitante, string? estado, int pagina, int tamano, CancellationToken cancelacion);
}

/// <summary>Un pedido por su número (<c>PED-000123</c>). El de otro cliente responde como si no existiera.</summary>
public interface IObtenerPedido
{
    Task<Resultado<PedidoDto>> Ejecutar(Solicitante solicitante, string numero, CancellationToken cancelacion);
}

/// <summary>Registra un pedido a nombre de quien lo pide.</summary>
public interface ICrearPedido
{
    Task<Resultado<PedidoDto>> Ejecutar(Solicitante solicitante, CrearPedidoSolicitud solicitud, CancellationToken cancelacion);
}

/// <summary>Pasa un pedido a su estado siguiente (solo operador y admin: lo exige el controlador).</summary>
public interface IAvanzarPedido
{
    Task<Resultado<PedidoDto>> Ejecutar(Solicitante solicitante, string numero, CancellationToken cancelacion);
}
