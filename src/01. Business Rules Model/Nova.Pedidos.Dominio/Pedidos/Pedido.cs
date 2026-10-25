using Nova.Pedidos.Dominio.Comun;

namespace Nova.Pedidos.Dominio.Pedidos;

/// <summary>
/// Raíz del agregado Pedido: la única puerta para cambiarlo.
/// Las reglas del negocio (docs/modulos/pedidos.md) se escriben AQUÍ, no en el controlador ni en la base.
/// Si una regla se rompe: <c>throw new ExcepcionDominio("pedido.codigo", "mensaje para personas")</c>.
/// </summary>
public sealed class Pedido : RaizAgregado<Guid>
{
    private readonly List<LineaPedido> _lineas = [];

    private Pedido()
    {
    }

    public NumeroPedido Numero { get; private set; } = null!;

    /// <summary>El dueño: el <c>sub</c> del token de quien lo creó, nunca un dato que manda el cliente.</summary>
    public Guid ClienteId { get; private set; }

    public DateTimeOffset Fecha { get; private set; }

    public EstadoPedido Estado { get; private set; }

    public IReadOnlyList<LineaPedido> Lineas => _lineas.AsReadOnly();

    /// <summary>Lo calcula el pedido con sus líneas; nunca viene de afuera.</summary>
    public decimal Total => _lineas.Sum(l => l.Subtotal);

    /// <summary>Un pedido nuevo nace Registrado y con al menos una línea.</summary>
    public static Pedido Registrar(NumeroPedido numero, Guid clienteId, DateTimeOffset fecha, IEnumerable<LineaPedido>? lineas)
    {
        ArgumentNullException.ThrowIfNull(numero);
        var todas = lineas?.ToList() ?? [];
        if (todas.Count == 0)
        {
            throw new ExcepcionDominio("pedido.sin-lineas", "Un pedido lleva al menos una línea.");
        }

        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            ClienteId = clienteId,
            Fecha = fecha,
            Estado = EstadoPedido.Registrado,
        };
        pedido._lineas.AddRange(todas);
        return pedido;
    }

    /// <summary>Pasa al estado siguiente, en orden. Un pedido Entregado ya no avanza.</summary>
    public void Avanzar()
    {
        Estado = Estado switch
        {
            EstadoPedido.Registrado => EstadoPedido.Pagado,
            EstadoPedido.Pagado => EstadoPedido.Enviado,
            EstadoPedido.Enviado => EstadoPedido.Entregado,
            _ => throw new ExcepcionDominio("pedido.no-avanza", $"El pedido {Numero} está {Estado} y ya no avanza."),
        };
    }

    public bool EsDe(Guid clienteId) => ClienteId == clienteId;
}
