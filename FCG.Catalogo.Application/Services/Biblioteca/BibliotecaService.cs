using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Biblioteca;
using FCG.Catalogo.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FCG.Catalogo.Application.Services.Biblioteca
{
    public class BibliotecaService : IBibliotecaService
    {
        private const string CacheKeyBibliotecaPrefix = "catalogo:biblioteca:";

        private readonly IBibliotecaRepository _bibliotecaRepository;
        private readonly IDistributedCache _cache;

        public BibliotecaService(IBibliotecaRepository bibliotecaRepository, IDistributedCache cache)
        {
            _bibliotecaRepository = bibliotecaRepository;
            _cache = cache;
        }

        public async Task<IEnumerable<BibliotecaResponse>> ObterBibliotecaAsync(Guid usuarioId)
        {
            var cacheKey = $"{CacheKeyBibliotecaPrefix}{usuarioId}";
            var cacheado = await _cache.GetStringAsync(cacheKey);

            if (cacheado is not null)
            {
                NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", true);
                return JsonSerializer.Deserialize<List<BibliotecaResponse>>(cacheado) ?? new List<BibliotecaResponse>();
            }

            NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("cacheHit", false);

            var biblioteca = await _bibliotecaRepository.ObterPorUsuarioIdAsync(usuarioId);

            var response = biblioteca.Select(b => new BibliotecaResponse
            {
                JogoId = b.JogoId,
                Titulo = b.Jogo?.Titulo ?? string.Empty,
                Genero = b.Jogo?.Genero.ToString() ?? string.Empty,
                PrecoPago = b.PrecoPago,
                DataAquisicao = b.DataAquisicao
            }).ToList();

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) });

            return response;
        }
    }
}