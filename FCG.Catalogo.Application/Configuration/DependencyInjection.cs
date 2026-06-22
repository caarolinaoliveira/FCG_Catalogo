using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Services;
using FCG.Catalogo.Application.Services.Pedidos;
using FCG.Catalogo.Application.Services.Biblioteca;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Catalogo.Application.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IJogoService, JogoService>();
            services.AddScoped<IBibliotecaService, BibliotecaService>();
            services.AddScoped<IPedidoService, PedidoService>();

            return services;
        }
    }
}