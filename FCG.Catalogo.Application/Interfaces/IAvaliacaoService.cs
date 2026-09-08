using FCG.Catalogo.Application.Requests.Avaliacoes;
using FCG.Catalogo.Application.Responses.Avaliacoes;

namespace FCG.Catalogo.Application.Interfaces
{
    public interface IAvaliacaoService
    {
        Task<AvaliacaoResponse> CriarAsync(Guid jogoId, CriarAvaliacaoRequest request);
        Task<AvaliacaoResumoResponse> ObterPorJogoIdAsync(Guid jogoId);
    }
}
