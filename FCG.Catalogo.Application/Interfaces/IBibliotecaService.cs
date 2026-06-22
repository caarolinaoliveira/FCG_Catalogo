using FCG.Catalogo.Application.Responses.Biblioteca;

namespace FCG.Catalogo.Application.Interfaces
{
    public interface IBibliotecaService
    {
        Task<IEnumerable<BibliotecaResponse>> ObterBibliotecaAsync(Guid usuarioId);
    }
}