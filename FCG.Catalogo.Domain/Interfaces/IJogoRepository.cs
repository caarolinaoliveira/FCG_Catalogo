using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Domain.Interfaces

{
    public interface IJogoRepository : IRepository<JogoEntity>
    {
            Task<List<JogoEntity>> ObterPorGeneroAsync(JogoGeneroEnum genero);
            Task<List<JogoEntity>> ObterPorStatusAsync(JogoStatusEnum status);
            Task<List<JogoEntity>> ObterPorPrecoAsync(decimal precoMinimo, decimal precoMaximo);
            Task<List<JogoEntity>> ObterPorDataLancamentoAsync(DateTime dataInicio, DateTime dataFim);
            Task<JogoEntity?> ObterPorTituloAsync(string titulo);
            Task DeletarAsync(JogoEntity jogo);

    }
}