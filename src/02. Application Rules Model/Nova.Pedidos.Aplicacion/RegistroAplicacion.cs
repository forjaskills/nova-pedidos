using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nova.Pedidos.Aplicacion.Pedidos;

namespace Nova.Pedidos.Aplicacion;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        // La hora la da un TimeProvider (las pruebas pueden fijarla); nunca DateTime.Now.
        servicios.TryAddSingleton(TimeProvider.System);

        // Casos de uso: nuevo-modulo agrega aquí los de cada módulo.
        // módulo Pedidos
        servicios.AddScoped<IListarPedidos, ListarPedidos>();
        servicios.AddScoped<IObtenerPedido, ObtenerPedido>();
        servicios.AddScoped<ICrearPedido, CrearPedido>();
        servicios.AddScoped<IAvanzarPedido, AvanzarPedido>();
        return servicios;
    }
}
