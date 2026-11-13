using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace  FCG.Catalogo.Application.Services
{
    public class JogoService : IJogoService
    {
        private const string CacheKeyTodosJogos = "catalogo:jogos:todos";

        private readonly IJogoRepository _jogoRepository;
        private readonly IDistributedCache _cache;
        private readonly IJogoIndexador _indexador;
        private readonly IJogoBuscaService _busca;
        private readonly ILogger<JogoService> _logger;

        public JogoService(
            IJogoRepository jogoRepository,
            IDistributedCache cache,
            IJogoIndexador indexador,
            IJogoBuscaService busca,
            ILogger<JogoService> logger)
        {
            _jogoRepository = jogoRepository;
            _cache = cache;
            _indexador = indexador;
            _busca = busca;
            _logger = logger;
        }

        // O cache é só otimização: se o Redis falhar, o fluxo segue pelo banco.
        private async Task InvalidarCacheAsync()
        {
            try
            {
                await _cache.RemoveAsync(CacheKeyTodosJogos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao invalidar o cache da listagem de jogos; ele expira sozinho em até 60s");
            }
        }

        private async Task<List<JogoResponse>?> LerListagemDoCacheAsync()
        {
            try
            {
                var cacheado = await _cache.GetStringAsync(CacheKeyTodosJogos);
                return cacheado is null ? null : JsonSerializer.Deserialize<List<JogoResponse>>(cacheado) ?? new List<JogoResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao ler o cache da listagem de jogos; consultando o banco");
                return null;
            }
        }

        private async Task GravarListagemNoCacheAsync(List<JogoResponse> jogos)
        {
            try
            {
                await _cache.SetStringAsync(CacheKeyTodosJogos, JsonSerializer.Serialize(jogos),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao gravar o cache da listagem de jogos");
            }
        }

        public Task<BuscaJogosResponse> BuscarAsync(string termo, int pagina, int tamanho, CancellationToken cancellationToken = default)
            => _busca.BuscarAsync(termo, pagina, tamanho, cancellationToken);

        public async Task<JogoResponse> CriarJogoAsync(CriarJogoRequest request)
        {
            if (await _jogoRepository.ObterPorTituloAsync(request.Titulo) != null)
                throw new ConflictException("Já existe um jogo com este título.");

            var jogo = new JogoEntity
            {
                Titulo= request.Titulo,
                Descricao = request.Descricao,
                Genero = request.Genero,
                Preco = request.Preco,
                DataLancamento = request.DataLancamento,
                DataCriacao= DateTime.UtcNow
            };

            await _jogoRepository.AdicionarAsync(jogo);
            await InvalidarCacheAsync();
            await _indexador.IndexarAsync(jogo);

            return new JogoResponse
            {
                Id = jogo.Id,
                Titulo = jogo.Titulo,
                Descricao = jogo.Descricao,
                Genero = jogo.Genero.ToString(),
                Preco = jogo.Preco,
                DataLancamento = jogo.DataLancamento,
                DataCriacao = jogo.DataCriacao
            };
        }

        public async Task<JogoResponse> AtualizarJogoAsync(Guid id, AtualizarJogoRequest request)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(id);

            if (jogo is null)
                throw new NotFoundException("Jogo não encontrado.");

            if (request.Titulo is not null)
                jogo.Titulo = request.Titulo;

            if (request.Descricao is not null)
                jogo.Descricao = request.Descricao;

            if (request.Preco.HasValue)
                jogo.Preco = request.Preco.Value;

            if (request.DataLancamento.HasValue)
                jogo.DataLancamento = request.DataLancamento.Value;

            if (request.Genero.HasValue)
                jogo.Genero = request.Genero.Value;

            await _jogoRepository.AtualizarAsync(jogo);
            await InvalidarCacheAsync();
            await _indexador.IndexarAsync(jogo);

            return new JogoResponse
            {
                Id = jogo.Id,
                Titulo = jogo.Titulo,
                Descricao = jogo.Descricao,
                Genero = jogo.Genero.ToString(),
                Preco = jogo.Preco,
                DataLancamento = jogo.DataLancamento,
                DataCriacao = jogo.DataCriacao
            };
        }

        public async Task<List<JogoResponse>> ObterTodosAsync()
        {
            var cacheado = await LerListagemDoCacheAsync();
            if (cacheado is not null)
            {
                NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", true);
                return cacheado;
            }

            NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", false);

            var jogos = await _jogoRepository.ObterTodosAsync();

            var response = jogos.Select(jogo => new JogoResponse
            {
                Id = jogo.Id,
                Titulo = jogo.Titulo,
                Descricao = jogo.Descricao,
                Genero = jogo.Genero.ToString(),
                Preco = jogo.Preco,
                DataLancamento = jogo.DataLancamento,
                DataCriacao = jogo.DataCriacao
            }).ToList();

            await GravarListagemNoCacheAsync(response);

            return response;
        }

        public async Task<JogoResponse> ObterJogoPorIdAsync(Guid id)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(id);

            if (jogo == null)
                throw new NotFoundException("Jogo não encontrado.");

            return new JogoResponse
            {
                Id = jogo.Id,
                Titulo = jogo.Titulo,
                Descricao = jogo.Descricao,
                Genero = jogo.Genero.ToString(),
                Preco = jogo.Preco,
                DataLancamento = jogo.DataLancamento,
                DataCriacao = jogo.DataCriacao
            };
        }

        public async Task<JogoResponse> ObterJogoPorTituloAsync(string titulo)
        {
            var jogo = await _jogoRepository.ObterPorTituloAsync(titulo);

            if (jogo == null)
                throw new NotFoundException("Jogo não encontrado.");
                
            return new JogoResponse
            {
                Id = jogo.Id,
                Titulo = jogo.Titulo,
                Descricao = jogo.Descricao,
                Genero = jogo.Genero.ToString(),
                Preco = jogo.Preco,
                DataLancamento = jogo.DataLancamento,
                DataCriacao = jogo.DataCriacao
            };
        }


        public async Task DeletarJogoAsync(Guid id)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(id);

            if (jogo == null)
                throw new NotFoundException("Jogo não encontrado.");

            await _jogoRepository.DeletarAsync(jogo);
            await InvalidarCacheAsync();
            await _indexador.RemoverAsync(jogo.Id);
        }

        

    }
}