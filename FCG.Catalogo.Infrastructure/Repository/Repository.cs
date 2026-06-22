using System.Linq.Expressions;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Catalogo.Infrastructure.Repository
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
    {
        protected readonly CatalogoDbContext Db;
        protected readonly DbSet<TEntity> DbSet;

        protected Repository(CatalogoDbContext db)
        {
            Db = db;
            DbSet = db.Set<TEntity>();
        }

        public virtual async Task AdicionarAsync(TEntity entity)
        {
            DbSet.Add(entity);
            await SaveChangesAsync();
        }

        public virtual async Task<TEntity?> ObterPorIdAsync(Guid id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual async Task<List<TEntity>> ObterTodosAsync()
        {
            return await DbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task AtualizarAsync(TEntity entity)
        {
            DbSet.Update(entity);
            await SaveChangesAsync();
        }

        public virtual async Task RemoverAsync(Guid id)
        {
            var entity = DbSet.Local.FirstOrDefault(e => e.Id == id) 
                         ?? await DbSet.FindAsync(id);

            if (entity != null)
            {
                DbSet.Remove(entity);
                await SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TEntity>> ObterPorFiltroAsync(Expression<Func<TEntity, bool>> filtro)
        {
            return await DbSet.AsNoTracking().Where(filtro).ToListAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await Db.SaveChangesAsync();
        }

        public void Dispose()
        {
            Db?.Dispose();
        }
    }
}