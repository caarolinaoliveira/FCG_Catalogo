using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;


namespace  FCG.Catalogo.Application.Services
{
    public class JogoService : IJogoService
    {
        private const string CacheKeyTodosJogos = "catalogo:jogos:todos";

        private readonly IJogoRepository _jogoRepository;
        private readonly IDistributedCache _cache;

        public JogoService(IJogoRepository jogoRepository, IDistributedCache cache)
        {
            _jogoRepository = jogoRepository;
            _cache = cache;
        }

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
            await _cache.RemoveAsync(CacheKeyTodosJogos);

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
            await _cache.RemoveAsync(CacheKeyTodosJogos);

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
            var cacheado = await _cache.GetStringAsync(CacheKeyTodosJogos);
            if (cacheado is not null)
            {
                NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", true);
                return JsonSerializer.Deserialize<List<JogoResponse>>(cacheado) ?? new List<JogoResponse>();
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

            await _cache.SetStringAsync(CacheKeyTodosJogos, JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });

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
            await _cache.RemoveAsync(CacheKeyTodosJogos);
        }

        

    }
}