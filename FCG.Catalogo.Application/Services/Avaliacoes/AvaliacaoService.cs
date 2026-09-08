using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Requests.Avaliacoes;
using FCG.Catalogo.Application.Responses.Avaliacoes;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Domain.Interfaces;

namespace FCG.Catalogo.Application.Services.Avaliacoes
{
    public class AvaliacaoService : IAvaliacaoService
    {
        private readonly IAvaliacaoRepository _avaliacaoRepository;
        private readonly IJogoRepository _jogoRepository;

        public AvaliacaoService(IAvaliacaoRepository avaliacaoRepository, IJogoRepository jogoRepository)
        {
            _avaliacaoRepository = avaliacaoRepository;
            _jogoRepository = jogoRepository;
        }

        public async Task<AvaliacaoResponse> CriarAsync(Guid jogoId, CriarAvaliacaoRequest request)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(jogoId);
            if (jogo is null)
                throw new NotFoundException("Jogo não encontrado.");

            var avaliacao = new AvaliacaoEntity
            {
                JogoId = jogoId,
                UsuarioId = request.UsuarioId,
                Nota = request.Nota,
                Comentario = request.Comentario ?? string.Empty,
                CriadoEm = DateTime.UtcNow
            };

            await _avaliacaoRepository.AdicionarAsync(avaliacao);

            return new AvaliacaoResponse
            {
                Id = avaliacao.Id,
                JogoId = avaliacao.JogoId,
                UsuarioId = avaliacao.UsuarioId,
                Nota = avaliacao.Nota,
                Comentario = avaliacao.Comentario,
                CriadoEm = avaliacao.CriadoEm
            };
        }

        public async Task<AvaliacaoResumoResponse> ObterPorJogoIdAsync(Guid jogoId)
        {
            var avaliacoes = await _avaliacaoRepository.ObterPorJogoIdAsync(jogoId);

            var respostas = avaliacoes.Select(a => new AvaliacaoResponse
            {
                Id = a.Id,
                JogoId = a.JogoId,
                UsuarioId = a.UsuarioId,
                Nota = a.Nota,
                Comentario = a.Comentario,
                CriadoEm = a.CriadoEm
            }).ToList();

            return new AvaliacaoResumoResponse
            {
                MediaNotas = respostas.Count > 0 ? respostas.Average(a => a.Nota) : 0,
                TotalAvaliacoes = respostas.Count,
                Avaliacoes = respostas
            };
        }
    }
}
