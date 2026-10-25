using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nova.Pedidos.Dominio.Pedidos;

namespace Nova.Pedidos.Adaptadores.Gateways.Pedidos;

/// <summary>Cómo se guarda un Pedido en las tablas que crea bd/ (EF no crea ni cambia tablas).</summary>
internal sealed class ConfiguracionPedido : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> pedido)
    {
        pedido.ToTable("Pedidos");
        pedido.HasKey(e => e.Id);
        pedido.Property(e => e.Id).ValueGeneratedNever();
        pedido.Property(e => e.Numero)
            .HasConversion(n => n.Valor, v => NumeroPedido.Desde(v))
            .ValueGeneratedNever();
        pedido.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        pedido.Ignore(e => e.Total);

        pedido.OwnsMany(e => e.Lineas, linea =>
        {
            linea.ToTable("LineasPedido");
            linea.WithOwner().HasForeignKey("PedidoId");
            linea.Property<int>("Id").UseIdentityColumn();
            linea.HasKey("Id");
            linea.Property(l => l.Producto).HasMaxLength(LineaPedido.LargoMaximoProducto);
            linea.Property(l => l.PrecioUnitario).HasPrecision(18, 2);
            linea.Ignore(l => l.Subtotal);
        });
        pedido.Navigation(e => e.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
