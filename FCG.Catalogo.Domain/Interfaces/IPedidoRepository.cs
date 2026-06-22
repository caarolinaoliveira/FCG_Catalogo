using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Domain.Interfaces
{
    public interface IPedidoRepository : IRepository<PedidoEntity>
    {
        Task<List<PedidoEntity>> ObterPorUsuarioIdAsync(Guid usuarioId);
        Task<List<PedidoEntity>> ObterPorStatusAsync(PedidoStatusEnum status);
        Task<PedidoEntity?> ObterPorUsuarioEJogoAsync(Guid usuarioId, Guid jogoId);
    }
}