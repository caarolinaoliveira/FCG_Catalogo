using FCG.Catalogo.Domain.Entities;

namespace FCG.Catalogo.Domain.Interfaces
{
    public interface IAvaliacaoRepository
    {
        Task AdicionarAsync(AvaliacaoEntity avaliacao);
        Task<List<AvaliacaoEntity>> ObterPorJogoIdAsync(Guid jogoId);
    }
}
