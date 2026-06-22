using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalogo.Infrastructure.Repository
{
    public class PedidoRepository : Repository<PedidoEntity>, IPedidoRepository
    {
        public PedidoRepository(CatalogoDbContext db) : base(db)
        {
        }

        public async Task<List<PedidoEntity>> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            return await DbSet.AsNoTracking()
                .Include(p => p.Jogo)
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.CriadoEm)
                .ToListAsync();
        }

        public async Task<List<PedidoEntity>> ObterPorStatusAsync(PedidoStatusEnum status)
        {
            return await DbSet.AsNoTracking()
                .Include(p => p.Jogo)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CriadoEm)
                .ToListAsync();
        }

        public async Task<PedidoEntity?> ObterPorUsuarioEJogoAsync(Guid usuarioId, Guid jogoId)
        {
            return await DbSet
                .AsTracking()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.JogoId == jogoId);
        }
    }
}