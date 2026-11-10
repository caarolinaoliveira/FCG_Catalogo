using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Products.Elasticsearch;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FCG.Catalogo.Infrastructure.Search
{
    public sealed class ElasticsearchJogoIndice : IJogoIndexador, IJogoBuscaService
    {
        public const string Indice = "jogos";
        private const int TamanhoLoteBulk = 500;

        // Criar o índice e fazer o bulk levam mais que as operações interativas, que usam o timeout curto do cliente (3s).
        private static readonly TimeSpan TimeoutSincronizacao = TimeSpan.FromSeconds(60);

        private readonly ElasticsearchClient _client;
        private readonly ILogger<ElasticsearchJogoIndice> _logger;

        // Após uma falha de transporte, recusa as chamadas por 30s para não pagar o timeout a cada requisição.
        private static readonly TimeSpan JanelaCircuitoAberto = TimeSpan.FromSeconds(30);
        private long _circuitoAbertoAteTicks;

        private bool CircuitoAberto => DateTime.UtcNow.Ticks < Interlocked.Read(ref _circuitoAbertoAteTicks);
        private void AbrirCircuito() => Interlocked.Exchange(ref _circuitoAbertoAteTicks, DateTime.UtcNow.Add(JanelaCircuitoAberto).Ticks);
        private void FecharCircuito() => Interlocked.Exchange(ref _circuitoAbertoAteTicks, 0);

        private static bool FalhaDeDisponibilidade(ElasticsearchResponse r)
            => r.ApiCallDetails.HttpStatusCode is null or >= 500;

        public ElasticsearchJogoIndice(ElasticsearchClient client, ILogger<ElasticsearchJogoIndice> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task IndexarAsync(JogoEntity jogo)
        {
            if (CircuitoAberto)
            {
                _logger.LogDebug("[BUSCA] Indexação do jogo {JogoId} ignorada: Elasticsearch indisponível (circuito aberto). A reconciliação corrigirá o índice.", jogo.Id);
                return;
            }

            try
            {
                var resposta = await _client.IndexAsync(JogoDocumento.De(jogo), i => i.Index(Indice).Id(jogo.Id));

                if (resposta.IsValidResponse)
                {
                    FecharCircuito();
                    return;
                }

                if (FalhaDeDisponibilidade(resposta)) AbrirCircuito();
                _logger.LogWarning("[BUSCA] Falha ao indexar o jogo {JogoId}: {Motivo}. O índice será corrigido na próxima reconciliação.", jogo.Id, Motivo(resposta));
            }
            catch (Exception ex)
            {
                AbrirCircuito();
                _logger.LogWarning(ex, "[BUSCA] Elasticsearch indisponível ao indexar o jogo {JogoId}; o índice será corrigido na próxima reconciliação.", jogo.Id);
            }
        }

        public async Task RemoverAsync(Guid id)
        {
            if (CircuitoAberto)
            {
                _logger.LogDebug("[BUSCA] Remoção do jogo {JogoId} ignorada: Elasticsearch indisponível (circuito aberto). A reconciliação corrigirá o índice.", id);
                return;
            }

            try
            {
                // A sobrecarga DeleteAsync(string, Guid) é ambígua e não remove o documento; por isso a requisição explícita.
                var resposta = await _client.DeleteAsync(new DeleteRequest(Indice, id.ToString()));

                // 404: o documento já não estava no índice.
                if (resposta.IsValidResponse || resposta.ApiCallDetails.HttpStatusCode == 404)
                {
                    FecharCircuito();
                    return;
                }

                if (FalhaDeDisponibilidade(resposta)) AbrirCircuito();
                _logger.LogWarning("[BUSCA] Falha ao remover o jogo {JogoId} do índice: {Motivo}. O índice será corrigido na próxima reconciliação.", id, Motivo(resposta));
            }
            catch (Exception ex)
            {
                AbrirCircuito();
                _logger.LogWarning(ex, "[BUSCA] Elasticsearch indisponível ao remover o jogo {JogoId}; o índice será corrigido na próxima reconciliação.", id);
            }
        }

        public async Task<BuscaJogosResponse> BuscarAsync(string termo, int pagina, int tamanho, CancellationToken cancellationToken = default)
        {
            if (CircuitoAberto)
                throw new BuscaIndisponivelException("O serviço de busca está indisponível no momento. Tente novamente em instantes.");

            SearchResponse<JogoDocumento> resposta;
            try
            {
                resposta = await _client.SearchAsync<JogoDocumento>(s => s
                    .Index(Indice)
                    .From((pagina - 1) * tamanho)
                    .Size(tamanho)
                    .TrackTotalHits(new TrackHits(true)) // total exato (o padrão trunca em 10.000)
                    .Query(q => q.MultiMatch(m => m
                        .Query(termo)
                        .Fields(new[] { "titulo^3", "descricao" })
                        // AUTO: até 2 letras exatas, 3 a 5 letras com 1 erro, 6 ou mais com 2 erros.
                        .Fuzziness(new Fuzziness("AUTO"))))
                    .Sort(o => o.Score(sc => sc.Order(SortOrder.Desc))),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AbrirCircuito();
                throw new BuscaIndisponivelException("O serviço de busca está indisponível no momento. Tente novamente em instantes.", ex);
            }

            if (!resposta.IsValidResponse)
            {
                if (FalhaDeDisponibilidade(resposta)) AbrirCircuito();
                _logger.LogWarning("[BUSCA] Busca falhou: {Motivo}", Motivo(resposta));
                throw new BuscaIndisponivelException("O serviço de busca está indisponível no momento. Tente novamente em instantes.");
            }

            FecharCircuito();

            var itens = resposta.Hits
                .Where(h => h.Source is not null)
                .Select(h => new JogoBuscaItemResponse
                {
                    Id = h.Source!.Id,
                    Titulo = h.Source.Titulo,
                    Descricao = h.Source.Descricao,
                    Genero = h.Source.Genero,
                    Preco = h.Source.Preco,
                    PrecoPromocional = h.Source.PrecoPromocional,
                    DataLancamento = h.Source.DataLancamento,
                    Score = h.Score ?? 0
                })
                .ToList();

            return new BuscaJogosResponse { Pagina = pagina, Tamanho = tamanho, Total = resposta.Total, Itens = itens };
        }

        public sealed record EstadoIndice(bool Existe, long Documentos);

        // Diferente do indexador, este método e o ReconstruirAsync lançam exceção: o chamador faz retry.
        public async Task<EstadoIndice> ObterEstadoAsync(CancellationToken cancellationToken)
        {
            // O HEAD devolve 404 quando o índice não existe: é resposta normal, não falha.
            var existe = await _client.Indices.ExistsAsync(Indice, d => d.RequestConfiguration(r => r.RequestTimeout(TimeoutSincronizacao)), cancellationToken);
            if (existe.ApiCallDetails.HttpStatusCode is not (200 or 404))
                throw new InvalidOperationException($"Falha ao verificar o índice: {Motivo(existe)}");

            FecharCircuito();

            if (!existe.Exists)
                return new EstadoIndice(false, 0);

            var contagem = await _client.CountAsync(c => c.Indices(Indice).RequestConfiguration(r => r.RequestTimeout(TimeoutSincronizacao)), cancellationToken);
            ValidarTransporte(contagem, "contar os documentos do índice");
            return new EstadoIndice(true, contagem.Count);
        }

        public async Task ReconstruirAsync(IReadOnlyCollection<JogoEntity> jogos, CancellationToken cancellationToken)
        {
            // Recriar, em vez de só completar, também remove documentos de jogos que já não existem no SQL.
            var remocao = await _client.Indices.DeleteAsync(Indice, d => d.RequestConfiguration(r => r.RequestTimeout(TimeoutSincronizacao)), cancellationToken);
            if (remocao.ApiCallDetails.HttpStatusCode is not (200 or 404))
                throw new InvalidOperationException($"Falha ao remover o índice defasado: {Motivo(remocao)}");

            await CriarIndiceAsync(cancellationToken);

            foreach (var lote in jogos.Chunk(TamanhoLoteBulk))
            {
                var documentos = lote.Select(JogoDocumento.De).ToList();
                var bulk = await _client.BulkAsync(b => b
                    .Index(Indice)
                    .IndexMany(documentos, (op, d) => op.Id(d.Id))
                    .RequestConfiguration(r => r.RequestTimeout(TimeoutSincronizacao)), cancellationToken);

                ValidarTransporte(bulk, "indexar em lote");
                if (bulk.Errors)
                    throw new InvalidOperationException($"Itens do lote bulk falharam: {bulk.ItemsWithErrors.Count()} erro(s).");
            }

            // Sem o refresh, os documentos só ficam buscáveis no refresh automático (1s).
            await _client.Indices.RefreshAsync(Indice, d => d.RequestConfiguration(r => r.RequestTimeout(TimeoutSincronizacao)), cancellationToken);
        }

        private async Task CriarIndiceAsync(CancellationToken cancellationToken)
        {
            var criacao = await _client.Indices.CreateAsync(Indice, c => c
                .RequestConfiguration(r => r.RequestTimeout(TimeoutSincronizacao))
                .Settings(s => s
                    // 1 nó: 1 shard e 0 réplicas (com réplica o índice ficaria "yellow" para sempre).
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    .Analysis(a => a.Analyzers(an => an
                        // minúsculas + remoção de acentos: "acao" encontra "Ação".
                        .Custom("texto", ca => ca.Tokenizer("standard").Filter(new[] { "lowercase", "asciifolding" })))))
                .Mappings(m => m.Properties(new Properties
                {
                    { "id", new KeywordProperty() },
                    {
                        "titulo", new TextProperty
                        {
                            Analyzer = "texto",
                            Fields = new Properties { { "keyword", new KeywordProperty { IgnoreAbove = 256 } } }
                        }
                    },
                    { "descricao", new TextProperty { Analyzer = "texto" } },
                    { "genero", new KeywordProperty() },
                    { "preco", new ScaledFloatNumberProperty { ScalingFactor = 100 } },
                    { "precoPromocional", new ScaledFloatNumberProperty { ScalingFactor = 100 } },
                    { "dataLancamento", new DateProperty() }
                })),
                cancellationToken);

            ValidarTransporte(criacao, "criar o índice");
        }

        private static void ValidarTransporte(ElasticsearchResponse resposta, string operacao)
        {
            if (!resposta.IsValidResponse)
                throw new InvalidOperationException($"Falha ao {operacao}: {Motivo(resposta)}");
        }

        private static string Motivo(ElasticsearchResponse resposta)
            => resposta.ElasticsearchServerError?.Error?.Reason
               ?? resposta.ApiCallDetails.OriginalException?.Message
               ?? $"HTTP {resposta.ApiCallDetails.HttpStatusCode?.ToString() ?? "sem resposta"}";
    }
}
