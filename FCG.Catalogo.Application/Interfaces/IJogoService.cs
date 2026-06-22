using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;

namespace  FCG.Catalogo.Application.Interfaces

{
    public interface IJogoService 
    {
        Task<List<JogoResponse>> ObterTodosAsync();
        Task<JogoResponse> ObterJogoPorTituloAsync(string titulo);
        Task<JogoResponse> ObterJogoPorIdAsync(Guid id);
        Task<JogoResponse> CriarJogoAsync(CriarJogoRequest jogo);
        Task<JogoResponse> AtualizarJogoAsync(Guid id, AtualizarJogoRequest jogo);
        Task DeletarJogoAsync(Guid id);

    }
}