using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Pedidos.Adaptadores.Gateways;

public static class RegistroGateways
{
    public static IServiceCollection AgregarGateways(this IServiceCollection servicios)
    {
        // La cadena sale de appsettings.{Ambiente}.json: autenticación de Windows, sin contraseñas.
        servicios.AddDbContext<PedidosDbContext>((proveedor, opciones) => opciones.UseSqlServer(
            proveedor.GetRequiredService<IConfiguration>().GetConnectionString("Principal")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Principal en appsettings.{Ambiente}.json")));
        servicios.AddHealthChecks().AddDbContextCheck<PedidosDbContext>("base-de-datos");

        // Repositorios: nuevo-modulo agrega aquí los de cada módulo.
        return servicios;
    }
}
