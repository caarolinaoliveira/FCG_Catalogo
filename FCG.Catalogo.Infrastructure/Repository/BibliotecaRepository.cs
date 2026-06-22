using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalogo.Infrastructure.Repository
{
    public class BibliotecaRepository : Repository<BibliotecaUsuarioEntity>, IBibliotecaRepository
    {
        public BibliotecaRepository(CatalogoDbContext db) : base(db)
        {
        }

        public async Task<IEnumerable<BibliotecaUsuarioEntity>> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            return await DbSet.AsNoTracking()
                .Include(b => b.Jogo)
                .Where(b => b.UsuarioId == usuarioId)
                .ToListAsync();
        }

        public async Task<BibliotecaUsuarioEntity?> ObterPorUsuarioEJogoAsync(Guid usuarioId, Guid jogoId)
        {
            return await DbSet.AsNoTracking()
                .Include(b => b.Jogo)
                .FirstOrDefaultAsync(b => b.UsuarioId == usuarioId && b.JogoId == jogoId);
        }
        public async Task<bool> UsuarioPossuiJogoAsync(Guid usuarioId, Guid jogoId)
        {
            return await DbSet.AnyAsync(b => b.UsuarioId == usuarioId && b.JogoId == jogoId);
        }
    }
}