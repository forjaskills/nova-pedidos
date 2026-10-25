using Microsoft.EntityFrameworkCore;
using Nova.Pedidos.Dominio.Pedidos;

namespace Nova.Pedidos.Adaptadores.Gateways.Pedidos;

/// <summary>Implementa el puerto del dominio con EF Core sobre SQL Server.</summary>
internal sealed class RepositorioPedidos(PedidosDbContext contexto) : IRepositorioPedidos
{
    public async Task<NumeroPedido> SiguienteNumero(CancellationToken cancelacion) =>
        NumeroPedido.Desde(await SiguienteSecuencia(cancelacion));

    public Task<Pedido?> ObtenerPorNumero(NumeroPedido numero, CancellationToken cancelacion) =>
        contexto.Set<Pedido>().SingleOrDefaultAsync(p => p.Numero == numero, cancelacion);

    public async Task<(IReadOnlyList<Pedido> Pedidos, int Total)> Listar(
        Guid? clienteId, EstadoPedido? estado, int pagina, int tamano, CancellationToken cancelacion)
    {
        var consulta = contexto.Set<Pedido>().AsNoTracking();
        if (clienteId is { } id)
        {
            consulta = consulta.Where(p => p.ClienteId == id);
        }

        if (estado is { } e)
        {
            consulta = consulta.Where(p => p.Estado == e);
        }

        var total = await consulta.CountAsync(cancelacion);
        var pedidos = await consulta
            .OrderByDescending(p => p.Numero)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);
        return (pedidos, total);
    }

    public void Agregar(Pedido pedido) => contexto.Add(pedido);

    public Task Guardar(CancellationToken cancelacion) => contexto.SaveChangesAsync(cancelacion);

    // SingleAsync() aquí no sirve: EF envuelve la consulta en una subconsulta y SQL Server rechaza NEXT VALUE FOR.
    private async Task<long> SiguienteSecuencia(CancellationToken cancelacion) =>
        (await contexto.Database
            .SqlQueryRaw<long>("SELECT NEXT VALUE FOR dbo.SecuenciaPedidos AS Value")
            .ToListAsync(cancelacion)).Single();
}
