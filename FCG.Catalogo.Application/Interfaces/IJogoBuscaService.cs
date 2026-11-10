using FCG.Catalogo.Application.Responses.Jogos;

namespace FCG.Catalogo.Application.Interfaces
{
    public interface IJogoBuscaService
    {
        Task<BuscaJogosResponse> BuscarAsync(string termo, int pagina, int tamanho, CancellationToken cancellationToken = default);
    }
}
