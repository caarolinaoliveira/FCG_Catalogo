using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Infrastructure.Context;
using FCG.Catalogo.Infrastructure.Messaging;
using FCG.Catalogo.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
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

            if (!BsonClassMap.IsClassMapRegistered(typeof(AvaliacaoEntity)))
            {
                BsonClassMap.RegisterClassMap<AvaliacaoEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.GetMemberMap(nameof(AvaliacaoEntity.JogoId)).SetSerializer(new GuidSerializer(BsonType.String));
                    cm.GetMemberMap(nameof(AvaliacaoEntity.UsuarioId)).SetSerializer(new GuidSerializer(BsonType.String));
                });
            }

            services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017"));

            services.AddSingleton(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase(configuration["MongoDB:Database"] ?? "FCG_Catalogo"));

            services.AddScoped<IAvaliacaoRepository, AvaliacaoRepository>();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379";
            });

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