using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Infrastructure.Context;
using FCG.Catalogo.Infrastructure.Messaging;
using FCG.Catalogo.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using RabbitMQ.Client;

namespace FCG.Catalogo.Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<CatalogoDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("FCG.Catalogo.Infrastructure")
                ));

            services.AddScoped<IJogoRepository, JogoRepository>();
            services.AddScoped<IBibliotecaRepository, BibliotecaRepository>();
            services.AddScoped<IPedidoRepository, PedidoRepository>();

            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:Usuario"] ?? "guest",
                    Password = configuration["RabbitMQ:Senha"] ?? "guest"
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddScoped<IMessagePublisher, RabbitMQPublisher>();
            services.AddSingleton(Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    ));

        services.AddHostedService<PaymentProcessedConsumer>();

    
            return services;
        }
    }
}