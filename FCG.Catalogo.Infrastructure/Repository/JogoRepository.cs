using FCG.Catalogo.Infrastructure.Context;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FCG.Catalogo.Infrastructure.Repository
{
    public class JogoRepository : Repository<JogoEntity>, IJogoRepository
    {
        public JogoRepository(CatalogoDbContext db) : base(db)
        {
        }

        public async Task<List<JogoEntity>> ObterPorGeneroAsync(JogoGeneroEnum genero)
        {
            return await DbSet.AsNoTracking()
                .Where(j => j.Genero == genero)
                .ToListAsync();
        }

        public async Task<List<JogoEntity>> ObterPorStatusAsync(JogoStatusEnum status)
        {
            return await DbSet.AsNoTracking()
                .Where(j => j.StatusJogo == status)
                .ToListAsync();
        }

        public async Task<List<JogoEntity>> ObterPorPrecoAsync(decimal precoMinimo, decimal precoMaximo)
        {
            return await DbSet.AsNoTracking()
                .Where(j => j.Preco >= precoMinimo && j.Preco <= precoMaximo)
                .ToListAsync();
        }

        public async Task<List<JogoEntity>> ObterPorDataLancamentoAsync(DateTime dataInicio, DateTime dataFim)
        {
            return await DbSet.AsNoTracking()
                .Where(j => j.DataLancamento >= dataInicio && j.DataLancamento <= dataFim)
                .ToListAsync();
        }
        public async Task<JogoEntity?> ObterPorTituloAsync(string titulo)
        {
            return await DbSet.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Titulo == titulo);
        }

        public async Task<JogoEntity?> ObterPorIdAsync(Guid id)
        {
            return await DbSet.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task DeletarAsync(JogoEntity jogo)
        {
            DbSet.Remove(jogo);
            await SaveChangesAsync();
        }
    }
}