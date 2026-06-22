using System.Linq.Expressions;
using FCG.Catalogo.Domain.Entities;

namespace FCG.Catalogo.Domain.Interfaces
{
    public interface IRepository<TEntity> : IDisposable where TEntity : Entity
    {
        Task AdicionarAsync(TEntity entity);
        Task<TEntity?> ObterPorIdAsync(Guid id);
        Task<List<TEntity>> ObterTodosAsync();
        Task AtualizarAsync(TEntity entity);
        Task RemoverAsync(Guid id);
        Task<int> SaveChangesAsync();
        Task<IEnumerable<TEntity>> ObterPorFiltroAsync(Expression<Func<TEntity, bool>> filtro);
    }
}