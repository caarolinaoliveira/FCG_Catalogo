using FCG.Catalogo.Application.Interfaces;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;

namespace FCG.Catalogo.Infrastructure.Messaging
{
    public class RabbitMQPublisher : IMessagePublisher
    {
        private readonly IConnection _connection;
        private readonly IMongoCollection<BsonDocument> _eventosDominio;
        private readonly ILogger<RabbitMQPublisher> _logger;

        public RabbitMQPublisher(IConnection connection, IMongoClient mongoClient, IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
        {
            _connection = connection;
            var eventLogDatabase = mongoClient.GetDatabase(configuration["MongoDB:EventLogDatabase"] ?? "FCG_EventLog");
            _eventosDominio = eventLogDatabase.GetCollection<BsonDocument>("eventos_dominio");
            _logger = logger;
        }

        public async Task PublicarAsync<T>(T evento, string fila) where T : class
        {
            using var channel = await _connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("dlx", ExchangeType.Direct, durable: true);

            await channel.QueueDeclareAsync(
                queue: $"{fila}.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            await channel.QueueBindAsync($"{fila}.dlq", "dlx", fila);

            var args = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", "dlx" },
                { "x-dead-letter-routing-key", fila }
            };

            await channel.QueueDeclareAsync(
                queue: fila,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            var json = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties { Persistent = true };

            if (Activity.Current is not null)
            {
                props.Headers = new Dictionary<string, object?>
                {
                    { "traceparent", Activity.Current.Id }
                };

                if (!string.IsNullOrEmpty(Activity.Current.TraceStateString))
                {
                    props.Headers["tracestate"] = Activity.Current.TraceStateString;
                }
            }

            props.Headers ??= new Dictionary<string, object?>();
            NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.InsertDistributedTraceHeaders(
                props.Headers, (headers, key, value) => headers[key] = value);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: fila,
                mandatory: false,
                basicProperties: props,
                body: body
            );

            await RegistrarEventoDominioAsync<T>(fila, json);
        }

        private async Task RegistrarEventoDominioAsync<T>(string fila, string json) where T : class
        {
            try
            {
                var documento = new BsonDocument
                {
                    { "tipoEvento", typeof(T).Name },
                    { "fila", fila },
                    { "origem", "FCG.Catalogo" },
                    { "correlationId", Activity.Current?.TraceId.ToString() ?? (BsonValue)BsonNull.Value },
                    { "publicadoEm", DateTime.UtcNow },
                    { "payload", BsonDocument.Parse(json) }
                };

                await _eventosDominio.InsertOneAsync(documento);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Falha ao registrar evento de domínio no Mongo | Tipo: {Tipo} | Fila: {Fila}",
                    typeof(T).Name, fila);
            }
        }
    }
}