using FCG.Catalogo.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FCG.Catalogo.Infrastructure.Search
{
    // A verificação é periódica porque, com emptyDir, o índice some se só o pod do Elasticsearch reiniciar.
    public sealed class JogoIndiceInicializador : BackgroundService
    {
        private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(60);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ElasticsearchJogoIndice _indice;
        private readonly ILogger<JogoIndiceInicializador> _logger;

        public JogoIndiceInicializador(
            IServiceScopeFactory scopeFactory,
            ElasticsearchJogoIndice indice,
            ILogger<JogoIndiceInicializador> logger)
        {
            _scopeFactory = scopeFactory;
            _indice = indice;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var espera = EsperaInicial;
            var falhasSeguidas = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan proximaVerificacao;
                try
                {
                    await VerificarAsync(stoppingToken);
                    if (falhasSeguidas > 0)
                        _logger.LogInformation("[BUSCA] Elasticsearch voltou a responder após {Falhas} tentativa(s) sem sucesso.", falhasSeguidas);

                    falhasSeguidas = 0;
                    espera = EsperaInicial;
                    proximaVerificacao = IntervaloVerificacao;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    falhasSeguidas++;
                    // Só a primeira falha da sequência vai para Warning; as repetições ficam em Debug para não inundar o log.
                    if (falhasSeguidas == 1)
                        _logger.LogWarning("[BUSCA] Verificação do índice falhou: {Erro}. Nova tentativa em {Espera}s (espera crescente até {Maxima}s).",
                            ex.Message, (int)espera.TotalSeconds, (int)EsperaMaxima.TotalSeconds);
                    else
                        _logger.LogDebug("[BUSCA] Verificação do índice ainda falhando (tentativa {Falhas}): {Erro}", falhasSeguidas, ex.Message);

                    proximaVerificacao = espera;
                    espera = TimeSpan.FromSeconds(Math.Min(espera.TotalSeconds * 2, EsperaMaxima.TotalSeconds));
                }

                try { await Task.Delay(proximaVerificacao, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        private async Task VerificarAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repositorio = scope.ServiceProvider.GetRequiredService<IJogoRepository>();

            var jogosNoSql = await repositorio.ContarAsync();
            var estado = await _indice.ObterEstadoAsync(cancellationToken);

            if (estado.Existe && estado.Documentos == jogosNoSql)
            {
                _logger.LogDebug("[BUSCA] Índice '{Indice}' consistente: {Documentos} documento(s).", ElasticsearchJogoIndice.Indice, estado.Documentos);
                return;
            }

            var motivo = !estado.Existe ? "o índice não existia" : $"contagem divergente (índice: {estado.Documentos}, SQL: {jogosNoSql})";
            var jogos = await repositorio.ObterTodosAsync();
            await _indice.ReconstruirAsync(jogos, cancellationToken);

            _logger.LogInformation("[BUSCA] Índice '{Indice}' reconstruído: {Jogos} jogo(s) indexado(s) em lote ({Motivo}).",
                ElasticsearchJogoIndice.Indice, jogos.Count, motivo);
        }
    }
}
