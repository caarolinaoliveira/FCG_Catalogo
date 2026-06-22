using System.Text;
using System.Text.Json;
using FCG.Catalogo.Application.Events;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Infrastructure.Messaging
{
    public class PaymentProcessedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentProcessedConsumer> _logger;
        private readonly IConnection _connection;
        private IChannel? _channel;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

        public PaymentProcessedConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentProcessedConsumer> logger,
            IConnection connection,
            AsyncCircuitBreakerPolicy circuitBreaker)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connection = connection;
            _circuitBreaker = circuitBreaker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync("dlx", ExchangeType.Direct, durable: true);

            await _channel.QueueDeclareAsync(
                queue: "payment.processed.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            await _channel.QueueBindAsync("payment.processed.dlq", "dlx", "payment.processed");

            var args = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", "dlx" },
                { "x-dead-letter-routing-key", "payment.processed" }
            };

            await _channel.QueueDeclareAsync(
                queue: "payment.processed",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                if (_circuitBreaker.CircuitState == CircuitState.Open)
                {
                    _logger.LogWarning("[CATALOGO] Circuito aberto — reenfileirando mensagem.");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var tentativas = 0;

                if (ea.BasicProperties.Headers != null &&
                    ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryObj))
                {
                    tentativas = Convert.ToInt32(retryObj);
                }

                try
                {
                    var evento = JsonSerializer.Deserialize<PaymentProcessedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evento is null)
                    {
                        _logger.LogWarning("[CATALOGO] Evento null — descartando.");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                   if (evento.Status == "Approved")
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var bibliotecaRepository = scope.ServiceProvider.GetRequiredService<IBibliotecaRepository>();
                        var pedidoRepository = scope.ServiceProvider.GetRequiredService<IPedidoRepository>();

                        var pedido = await pedidoRepository.ObterPorUsuarioEJogoAsync(evento.UserId, evento.GameId);
                        if (pedido != null)
                        {
                            pedido.AtualizarStatus(PedidoStatusEnum.Aprovado);
                            await pedidoRepository.AtualizarAsync(pedido);
                            await pedidoRepository.SaveChangesAsync();
                        }


                        var japossui = await bibliotecaRepository.UsuarioPossuiJogoAsync(evento.UserId, evento.GameId);
                        if (!japossui)
                        {
                            var biblioteca = new BibliotecaUsuarioEntity
                            {
                                UsuarioId = evento.UserId,
                                JogoId = evento.GameId,
                                PrecoPago = evento.Price,
                                DataAquisicao = DateTime.UtcNow
                            };
                            await bibliotecaRepository.AdicionarAsync(biblioteca);
                            await bibliotecaRepository.SaveChangesAsync();

                            _logger.LogInformation(
                                "[CATALOGO] Jogo adicionado à biblioteca | UserId: {UserId} | GameId: {GameId}",
                                evento.UserId, evento.GameId);
                        }
                    }
                    else
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var pedidoRepository = scope.ServiceProvider.GetRequiredService<IPedidoRepository>();

                        var pedido = await pedidoRepository.ObterPorUsuarioEJogoAsync(evento.UserId, evento.GameId);
                        if (pedido != null)
                        {
                            pedido.AtualizarStatus(PedidoStatusEnum.Rejeitado);
                            await pedidoRepository.AtualizarAsync(pedido);
                            await pedidoRepository.SaveChangesAsync();
                        }

                        _logger.LogInformation(
                            "[CATALOGO] Pagamento rejeitado — jogo não adicionado | UserId: {UserId} | GameId: {GameId}",
                            evento.UserId, evento.GameId);
                    }
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.LogError(ex, "[CATALOGO] Circuit breaker aberto — enviando para DLQ.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (Exception ex)
                {
                    tentativas++;

                    if (tentativas >= 3)
                    {
                        _logger.LogError(ex, "[CATALOGO] Máximo de tentativas — enviando para DLQ.");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    _logger.LogWarning("[CATALOGO] Tentativa {N} falhou — reenfileirando.", tentativas);

                    var props = new BasicProperties
                    {
                        Persistent = true,
                        Headers = new Dictionary<string, object?>
                        {
                            { "x-retry-count", tentativas }
                        }
                    };

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativas)));
                    await _channel.BasicPublishAsync("", "payment.processed", false, props, ea.Body);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            await _channel.BasicConsumeAsync("payment.processed", autoAck: false, consumer: consumer);
            _logger.LogInformation("[CATALOGO] Aguardando mensagens na fila payment.processed...");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.DisposeAsync().GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}