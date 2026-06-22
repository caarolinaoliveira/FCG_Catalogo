using FCG.Catalogo.Application.Events;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Pedidos;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;
using FCG.Catalogo.Domain.Interfaces;
using FCG.Catalogo.Domain.Exceptions;

namespace FCG.Catalogo.Application.Services.Pedidos
{
    public class PedidoService : IPedidoService
    {
        private readonly IJogoRepository _jogoRepository;
        private readonly IBibliotecaRepository _bibliotecaRepository;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IMessagePublisher _messagePublisher;

        public PedidoService(
            IJogoRepository jogoRepository,
            IBibliotecaRepository bibliotecaRepository,
            IPedidoRepository pedidoRepository,
            IMessagePublisher messagePublisher)
        {
            _jogoRepository = jogoRepository;
            _bibliotecaRepository = bibliotecaRepository;
            _pedidoRepository = pedidoRepository;
            _messagePublisher = messagePublisher;
        }

        public async Task IniciarCompraAsync(Guid jogoId, Guid usuarioId)
        {
            var jogo = await _jogoRepository.ObterPorIdAsync(jogoId);
            if (jogo == null)
                throw new Exception($"Jogo {jogoId} não encontrado.");

            var japossui = await _bibliotecaRepository.UsuarioPossuiJogoAsync(usuarioId, jogoId);
            if (japossui)
                throw new ConflictException("Usuário já possui este jogo.");

            var pedido = new PedidoEntity
            {
                UsuarioId = usuarioId,
                JogoId = jogoId,
                Preco = jogo.Preco,
                Status = PedidoStatusEnum.AguardandoPagamento,
                CriadoEm = DateTime.UtcNow
            };

            await _pedidoRepository.AdicionarAsync(pedido);

            var evento = new OrderPlacedEvent
            {
                UserId = usuarioId,
                GameId = jogoId,
                Price = jogo.Preco,
                PlacedAt = DateTime.UtcNow
            };

            await _messagePublisher.PublicarAsync(evento, "order.placed");
        }

        public async Task<List<PedidoResponse>> ObterPorUsuarioIdAsync(Guid usuarioId)
        {
            var pedidos = await _pedidoRepository.ObterPorUsuarioIdAsync(usuarioId);

            return pedidos.Select(p => new PedidoResponse
            {
                Id = p.Id,
                JogoId = p.JogoId,
                Titulo = p.Jogo?.Titulo ?? string.Empty,
                Preco = p.Preco,
                Status = p.Status.ToString(),
                CriadoEm = p.CriadoEm,
                AtualizadoEm = p.AtualizadoEm
            }).ToList();
        }
    }
}