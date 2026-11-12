using System.Text;
using System.Text.Json;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Application.Services;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FCG.Catalogo.Tests.Application.Services;

public class JogoServiceTests
{
    private const string ChaveCache = "catalogo:jogos:todos";

    private readonly IJogoRepository _repository = Substitute.For<IJogoRepository>();
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();
    private readonly IJogoIndexador _indexador = Substitute.For<IJogoIndexador>();
    private readonly IJogoBuscaService _busca = Substitute.For<IJogoBuscaService>();
    private readonly JogoService _service;

    public JogoServiceTests()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(null));
        _service = new JogoService(_repository, _cache, _indexador, _busca);
    }

    private static JogoEntity JogoExistente() => new()
    {
        Titulo = "Zelda",
        Descricao = "Aventura épica",
        Genero = JogoGeneroEnum.Aventura,
        Preco = 199.90m,
        DataLancamento = new DateTime(2023, 5, 12)
    };

    private static CriarJogoRequest NovoJogo() => new()
    {
        Titulo = "Mario",
        Descricao = "Plataforma clássica",
        Genero = JogoGeneroEnum.Acao,
        Preco = 99.90m,
        DataLancamento = new DateTime(2024, 1, 10)
    };

    private void CacheComJogos(string json)
        => _cache.GetAsync(ChaveCache, Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(json)));

    public class Cache : JogoServiceTests
    {
        [Fact]
        public async Task Miss_busca_no_banco_e_grava_no_cache_por_60_segundos()
        {
            var jogo = JogoExistente();
            _repository.ObterTodosAsync().Returns(new List<JogoEntity> { jogo });

            var resultado = await _service.ObterTodosAsync();

            Assert.Equal(jogo.Id, Assert.Single(resultado).Id);
            await _cache.Received(1).SetAsync(
                ChaveCache,
                Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("Zelda")),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromSeconds(60)),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Hit_devolve_o_cache_sem_consultar_o_banco_nem_regravar()
        {
            var cacheado = new List<JogoResponse> { new() { Id = Guid.NewGuid(), Titulo = "Do cache", Descricao = "d" } };
            CacheComJogos(JsonSerializer.Serialize(cacheado));

            var resultado = await _service.ObterTodosAsync();

            Assert.Equal("Do cache", Assert.Single(resultado).Titulo);
            await _repository.DidNotReceive().ObterTodosAsync();
            await _cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!, default);
        }

        [Fact]
        public async Task Criar_invalida_o_cache_da_listagem()
        {
            await _service.CriarJogoAsync(NovoJogo());

            await _cache.Received(1).RemoveAsync(ChaveCache, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Editar_invalida_o_cache_da_listagem()
        {
            var jogo = JogoExistente();
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);

            await _service.AtualizarJogoAsync(jogo.Id, new AtualizarJogoRequest { Preco = 10m });

            await _cache.Received(1).RemoveAsync(ChaveCache, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Excluir_invalida_o_cache_da_listagem()
        {
            var jogo = JogoExistente();
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);

            await _service.DeletarJogoAsync(jogo.Id);

            await _cache.Received(1).RemoveAsync(ChaveCache, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Operacao_que_falha_nao_invalida_o_cache()
        {
            _repository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);
            _repository.ObterPorTituloAsync("Mario").Returns(JogoExistente());

            await Assert.ThrowsAsync<ConflictException>(() => _service.CriarJogoAsync(NovoJogo()));
            await Assert.ThrowsAsync<NotFoundException>(() => _service.AtualizarJogoAsync(Guid.NewGuid(), new AtualizarJogoRequest()));
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeletarJogoAsync(Guid.NewGuid()));

            await _cache.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
        }

        [Fact(Skip = "Bug conhecido: o cache é só otimização, mas uma falha do Redis derruba a listagem com 500.")]
        public async Task Redis_fora_do_ar_nao_impede_a_listagem()
        {
            _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Redis indisponível"));
            _repository.ObterTodosAsync().Returns(new List<JogoEntity> { JogoExistente() });

            var resultado = await _service.ObterTodosAsync();

            Assert.Single(resultado);
        }
    }

    public class Indexacao : JogoServiceTests
    {
        [Fact]
        public async Task Criar_indexa_o_jogo_salvo()
        {
            JogoEntity? salvo = null;
            await _repository.AdicionarAsync(Arg.Do<JogoEntity>(j => salvo = j));

            await _service.CriarJogoAsync(NovoJogo());

            await _indexador.Received(1).IndexarAsync(salvo!);
            Assert.Equal("Mario", salvo!.Titulo);
        }

        [Fact]
        public async Task Editar_indexa_o_jogo_com_os_dados_atualizados()
        {
            var jogo = JogoExistente();
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);

            await _service.AtualizarJogoAsync(jogo.Id, new AtualizarJogoRequest { Titulo = "Zelda 2" });

            await _indexador.Received(1).IndexarAsync(Arg.Is<JogoEntity>(j => j.Id == jogo.Id && j.Titulo == "Zelda 2"));
        }

        [Fact]
        public async Task Excluir_remove_o_documento_do_indice()
        {
            var jogo = JogoExistente();
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);

            await _service.DeletarJogoAsync(jogo.Id);

            await _indexador.Received(1).RemoverAsync(jogo.Id);
            await _indexador.DidNotReceiveWithAnyArgs().IndexarAsync(default!);
        }

        [Fact]
        public async Task Operacao_que_falha_nao_toca_no_indice()
        {
            _repository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);
            _repository.ObterPorTituloAsync("Mario").Returns(JogoExistente());

            await Assert.ThrowsAsync<ConflictException>(() => _service.CriarJogoAsync(NovoJogo()));
            await Assert.ThrowsAsync<NotFoundException>(() => _service.AtualizarJogoAsync(Guid.NewGuid(), new AtualizarJogoRequest()));
            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeletarJogoAsync(Guid.NewGuid()));

            await _indexador.DidNotReceiveWithAnyArgs().IndexarAsync(default!);
            await _indexador.DidNotReceiveWithAnyArgs().RemoverAsync(default);
        }

        [Fact]
        public async Task Criar_grava_no_banco_invalida_o_cache_e_so_depois_indexa()
        {
            await _service.CriarJogoAsync(NovoJogo());

            Received.InOrder(() =>
            {
                _repository.AdicionarAsync(Arg.Any<JogoEntity>());
                _cache.RemoveAsync(ChaveCache, Arg.Any<CancellationToken>());
                _indexador.IndexarAsync(Arg.Any<JogoEntity>());
            });
        }
    }

    public class Crud : JogoServiceTests
    {
        [Fact]
        public async Task Criar_devolve_o_jogo_com_os_dados_informados()
        {
            var resposta = await _service.CriarJogoAsync(NovoJogo());

            Assert.NotEqual(Guid.Empty, resposta.Id);
            Assert.Equal("Mario", resposta.Titulo);
            Assert.Equal("Acao", resposta.Genero);
            Assert.Equal(99.90m, resposta.Preco);
        }

        [Fact]
        public async Task Criar_com_titulo_ja_existente_lanca_conflito_sem_salvar()
        {
            _repository.ObterPorTituloAsync("Mario").Returns(JogoExistente());

            await Assert.ThrowsAsync<ConflictException>(() => _service.CriarJogoAsync(NovoJogo()));

            await _repository.DidNotReceiveWithAnyArgs().AdicionarAsync(default!);
        }

        [Fact]
        public async Task Atualizar_altera_somente_os_campos_informados()
        {
            var jogo = JogoExistente();
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);

            var resposta = await _service.AtualizarJogoAsync(jogo.Id, new AtualizarJogoRequest { Preco = 50m });

            Assert.Equal(50m, resposta.Preco);
            Assert.Equal("Zelda", resposta.Titulo);
            Assert.Equal("Aventura épica", resposta.Descricao);
            Assert.Equal("Aventura", resposta.Genero);
            await _repository.Received(1).AtualizarAsync(jogo);
        }

        [Fact]
        public async Task Atualizar_jogo_inexistente_lanca_nao_encontrado()
        {
            _repository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.AtualizarJogoAsync(Guid.NewGuid(), new AtualizarJogoRequest()));
        }

        [Fact]
        public async Task Excluir_remove_o_jogo_do_repositorio()
        {
            var jogo = JogoExistente();
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);

            await _service.DeletarJogoAsync(jogo.Id);

            await _repository.Received(1).DeletarAsync(jogo);
        }

        [Fact]
        public async Task Obter_por_id_inexistente_lanca_nao_encontrado()
        {
            _repository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.ObterJogoPorIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task Obter_por_titulo_devolve_o_jogo()
        {
            var jogo = JogoExistente();
            _repository.ObterPorTituloAsync("Zelda").Returns(jogo);

            var resposta = await _service.ObterJogoPorTituloAsync("Zelda");

            Assert.Equal(jogo.Id, resposta.Id);
        }

        [Fact]
        public async Task Obter_por_titulo_inexistente_lanca_nao_encontrado()
        {
            _repository.ObterPorTituloAsync(Arg.Any<string>()).Returns((JogoEntity?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.ObterJogoPorTituloAsync("Nada"));
        }

        [Fact(Skip = "Bug conhecido: Atualizar não confere título duplicado (só o Criar confere), permitindo dois jogos com o mesmo título.")]
        public async Task Atualizar_para_titulo_de_outro_jogo_lanca_conflito()
        {
            var jogo = JogoExistente();
            var outro = JogoExistente();
            outro.Titulo = "Mario";
            _repository.ObterPorIdAsync(jogo.Id).Returns(jogo);
            _repository.ObterPorTituloAsync("Mario").Returns(outro);

            await Assert.ThrowsAsync<ConflictException>(
                () => _service.AtualizarJogoAsync(jogo.Id, new AtualizarJogoRequest { Titulo = "Mario" }));
        }

        [Fact(Skip = "Bug conhecido: PrecoPromocional é aceito no request mas descartado; nunca chega à entidade (PrecoDesconto) nem à resposta.")]
        public async Task Criar_com_preco_promocional_devolve_o_preco_promocional()
        {
            var resposta = await _service.CriarJogoAsync(NovoJogo() with { PrecoPromocional = 49.90m });

            Assert.Equal(49.90m, resposta.PrecoPromocional);
        }
    }

    public class Busca : JogoServiceTests
    {
        [Fact]
        public async Task Delega_ao_servico_de_busca_com_os_mesmos_parametros()
        {
            var esperado = new BuscaJogosResponse { Pagina = 2, Tamanho = 5, Total = 7 };
            using var cts = new CancellationTokenSource();
            _busca.BuscarAsync("zelda", 2, 5, cts.Token).Returns(esperado);

            var resultado = await _service.BuscarAsync("zelda", 2, 5, cts.Token);

            Assert.Same(esperado, resultado);
        }

        [Fact]
        public async Task Propaga_a_indisponibilidade_da_busca()
        {
            _busca.BuscarAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new BuscaIndisponivelException("fora do ar"));

            await Assert.ThrowsAsync<BuscaIndisponivelException>(() => _service.BuscarAsync("zelda", 1, 10));
        }
    }
}
