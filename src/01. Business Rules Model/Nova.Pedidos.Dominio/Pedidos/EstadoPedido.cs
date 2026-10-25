namespace Nova.Pedidos.Dominio.Pedidos;

/// <summary>Por dónde pasa un pedido, en este orden. Solo avanza de a un paso (<see cref="Pedido.Avanzar"/>).</summary>
public enum EstadoPedido
{
    Registrado = 1,
    Pagado = 2,
    Enviado = 3,
    Entregado = 4,
}
