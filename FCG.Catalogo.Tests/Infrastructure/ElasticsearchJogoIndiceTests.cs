using System.Text;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Infrastructure.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace FCG.Catalogo.Tests.Infrastructure;

// O cliente usa um transporte em memória: nenhuma requisição sai para a rede.
public class ElasticsearchJogoIndiceTests
{
    private const string RespostaDeIndexacao =
        "{\"_index\":\"jogos\",\"_id\":\"x\",\"_version\":1,\"result\":\"created\",\"_shards\":{\"total\":1,\"successful\":1,\"failed\":0},\"_seq_no\":0,\"_primary_term\":1}";

    private static readonly JogoEntity Jogo = new()
    {
        Titulo = "Zelda",
        Descricao = "Aventura épica",
        Genero = JogoGeneroEnum.Aventura,
        Preco = 199.90m,
        DataLancamento = new DateTime(2023, 5, 12)
    };

    private sealed class Cenario(ElasticsearchJogoIndice indice, int[] contador)
    {
        public ElasticsearchJogoIndice Indice { get; } = indice;
        public int Requisicoes => contador[0];
    }

    private static Cenario Criar(int status, string corpo = "{}", bool lancarExcecoes = false)
    {
        var contador = new int[1];
        // O cliente só aceita respostas que trazem o cabeçalho de produto de um Elasticsearch real.
        var cabecalhos = new Dictionary<string, IEnumerable<string>> { ["x-elastic-product"] = new[] { "Elasticsearch" } };
        var invoker = new InMemoryRequestInvoker(Encoding.UTF8.GetBytes(corpo), status, null, "application/json", cabecalhos);
        var settings = new ElasticsearchClientSettings(new SingleNodePool(new Uri("http://localhost:9200")), invoker)
            .MaximumRetries(0)
            .OnRequestCompleted(_ => contador[0]++);
        if (lancarExcecoes) settings = settings.ThrowExceptions();

        var indice = new ElasticsearchJogoIndice(new ElasticsearchClient(settings), NullLogger<ElasticsearchJogoIndice>.Instance);
        return new Cenario(indice, contador);
    }

    public class ContratoDoIndexador
    {
        [Fact]
        public async Task Indexar_com_sucesso_nao_lanca()
        {
            var cenario = Criar(201, RespostaDeIndexacao);

            await cenario.Indice.IndexarAsync(Jogo);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(503)]
        [InlineData(400)]
        [InlineData(409)]
        public async Task Indexar_com_resposta_de_erro_nao_lanca(int status)
        {
            var cenario = Criar(status);

            await cenario.Indice.IndexarAsync(Jogo);
        }

        [Fact]
        public async Task Indexar_com_falha_de_transporte_nao_lanca()
        {
            var cenario = Criar(500, lancarExcecoes: true);

            await cenario.Indice.IndexarAsync(Jogo);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(404)]
        public async Task Remover_com_resposta_de_erro_ou_inexistente_nao_lanca(int status)
        {
            var cenario = Criar(status);

            await cenario.Indice.RemoverAsync(Guid.NewGuid());
        }

        [Fact]
        public async Task Remover_com_falha_de_transporte_nao_lanca()
        {
            var cenario = Criar(500, lancarExcecoes: true);

            await cenario.Indice.RemoverAsync(Guid.NewGuid());
        }

        [Fact]
        public async Task Apos_uma_falha_de_disponibilidade_o_circuito_abre_e_as_proximas_chamadas_nem_saem()
        {
            var cenario = Criar(503);

            await cenario.Indice.IndexarAsync(Jogo);
            var depoisDaPrimeira = cenario.Requisicoes;
            await cenario.Indice.IndexarAsync(Jogo);
            await cenario.Indice.RemoverAsync(Jogo.Id);

            Assert.Equal(1, depoisDaPrimeira);
            Assert.Equal(1, cenario.Requisicoes);
        }

        [Fact]
        public async Task Erro_do_cliente_nao_abre_o_circuito()
        {
            var cenario = Criar(400);

            await cenario.Indice.IndexarAsync(Jogo);
            await cenario.Indice.IndexarAsync(Jogo);

            Assert.Equal(2, cenario.Requisicoes);
        }
    }

    public class Busca
    {
        [Fact]
        public async Task Resposta_de_erro_do_servidor_vira_busca_indisponivel()
        {
            var cenario = Criar(503);

            await Assert.ThrowsAsync<BuscaIndisponivelException>(() => cenario.Indice.BuscarAsync("zelda", 1, 10));
        }

        [Fact]
        public async Task Falha_de_transporte_vira_busca_indisponivel()
        {
            var cenario = Criar(500, lancarExcecoes: true);

            await Assert.ThrowsAsync<BuscaIndisponivelException>(() => cenario.Indice.BuscarAsync("zelda", 1, 10));
        }

        [Fact]
        public async Task Com_o_circuito_aberto_falha_rapido_sem_chamar_o_elasticsearch()
        {
            var cenario = Criar(503);
            await Assert.ThrowsAsync<BuscaIndisponivelException>(() => cenario.Indice.BuscarAsync("zelda", 1, 10));

            await Assert.ThrowsAsync<BuscaIndisponivelException>(() => cenario.Indice.BuscarAsync("zelda", 1, 10));

            Assert.Equal(1, cenario.Requisicoes);
        }

        [Fact]
        public async Task Resposta_valida_vira_a_pagina_de_resultados_com_score()
        {
            var id = Guid.NewGuid();
            var corpo =
                "{\"took\":1,\"timed_out\":false,\"_shards\":{\"total\":1,\"successful\":1,\"skipped\":0,\"failed\":0}," +
                "\"hits\":{\"total\":{\"value\":1,\"relation\":\"eq\"},\"max_score\":2.5,\"hits\":[" +
                $"{{\"_index\":\"jogos\",\"_id\":\"{id}\",\"_score\":2.5,\"_source\":" +
                $"{{\"id\":\"{id}\",\"titulo\":\"Zelda\",\"descricao\":\"Aventura\",\"genero\":\"Aventura\",\"preco\":199.9,\"dataLancamento\":\"2023-05-12T00:00:00\"}}}}]}}}}";
            var cenario = Criar(200, corpo);

            var resposta = await cenario.Indice.BuscarAsync("zelda", 1, 10);

            Assert.Equal(1, resposta.Total);
            Assert.Equal(1, resposta.Pagina);
            Assert.Equal(10, resposta.Tamanho);
            var item = Assert.Single(resposta.Itens);
            Assert.Equal(id, item.Id);
            Assert.Equal("Zelda", item.Titulo);
            Assert.Equal(2.5, item.Score);
        }
    }
}
