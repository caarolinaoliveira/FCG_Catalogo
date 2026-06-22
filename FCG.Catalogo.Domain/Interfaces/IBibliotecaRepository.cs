using FCG.Catalogo.Domain.Entities;

namespace FCG.Catalogo.Domain.Interfaces
{
    public interface IBibliotecaRepository : IRepository<BibliotecaUsuarioEntity>
    {
        Task<IEnumerable<BibliotecaUsuarioEntity>> ObterPorUsuarioIdAsync(Guid usuarioId);
        Task<BibliotecaUsuarioEntity?> ObterPorUsuarioEJogoAsync(Guid usuarioId, Guid jogoId);
        Task<bool> UsuarioPossuiJogoAsync(Guid usuarioId, Guid jogoId);

    }
}