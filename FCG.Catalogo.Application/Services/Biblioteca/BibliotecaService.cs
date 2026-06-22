using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Biblioteca;
using FCG.Catalogo.Domain.Interfaces;

namespace FCG.Catalogo.Application.Services.Biblioteca
{
    public class BibliotecaService : IBibliotecaService
    {
        private readonly IBibliotecaRepository _bibliotecaRepository;

        public BibliotecaService(IBibliotecaRepository bibliotecaRepository)
        {
            _bibliotecaRepository = bibliotecaRepository;
        }

        public async Task<IEnumerable<BibliotecaResponse>> ObterBibliotecaAsync(Guid usuarioId)
        {
            var biblioteca = await _bibliotecaRepository.ObterPorUsuarioIdAsync(usuarioId);

            return biblioteca.Select(b => new BibliotecaResponse
            {
                JogoId = b.JogoId,
                Titulo = b.Jogo?.Titulo ?? string.Empty,
                Genero = b.Jogo?.Genero.ToString() ?? string.Empty,
                PrecoPago = b.PrecoPago,
                DataAquisicao = b.DataAquisicao
            });
        }
    }
}