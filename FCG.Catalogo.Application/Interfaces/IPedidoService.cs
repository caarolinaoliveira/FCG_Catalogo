using FCG.Catalogo.Application.Responses.Pedidos;

namespace FCG.Catalogo.Application.Interfaces
{
    public interface IPedidoService
    {
        Task IniciarCompraAsync(Guid jogoId, Guid usuarioId);
        Task<List<PedidoResponse>> ObterPorUsuarioIdAsync(Guid usuarioId);
    }
}