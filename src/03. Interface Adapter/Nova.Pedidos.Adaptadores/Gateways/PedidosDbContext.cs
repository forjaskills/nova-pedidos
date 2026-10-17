using Microsoft.EntityFrameworkCore;

namespace Nova.Pedidos.Adaptadores.Gateways;

/// <summary>Acceso a la base del servicio. Las tablas las crean los scripts de bd/ (Migrador), no EF.</summary>
public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> opciones) : DbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PedidosDbContext).Assembly);
}
