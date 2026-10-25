namespace Nova.Pedidos.Dominio.Pedidos;

/// <summary>
/// Puerto de infraestructura: lo que el dominio necesita para guardar y leer pedidos.
/// El dominio solo lo declara; la implementación (EF Core) vive en Adaptadores.Gateways.Pedidos.
/// </summary>
public interface IRepositorioPedidos
{
    /// <summary>El próximo número de la secuencia de la base.</summary>
    Task<NumeroPedido> SiguienteNumero(CancellationToken cancelacion);

    /// <summary>El pedido con ese número, listo para cambiarlo y guardarlo; null si no existe.</summary>
    Task<Pedido?> ObtenerPorNumero(NumeroPedido numero, CancellationToken cancelacion);

    /// <summary>Una página, del más nuevo al más viejo. <paramref name="clienteId"/> null: los de todos.</summary>
    Task<(IReadOnlyList<Pedido> Pedidos, int Total)> Listar(
        Guid? clienteId, EstadoPedido? estado, int pagina, int tamano, CancellationToken cancelacion);

    void Agregar(Pedido pedido);

    Task Guardar(CancellationToken cancelacion);
}
