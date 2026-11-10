using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/pedidos")]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        private readonly ILogger<PedidoController> _logger;

        public PedidoController(IPedidoService pedidoService, ILogger<PedidoController> logger)
        {
            _pedidoService = pedidoService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("{jogoId:guid}")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> RealizarPedido(Guid jogoId, [FromQuery] Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
            {
                _logger.LogWarning("Tentativa de pedido com UsuarioId vazio para o jogo {JogoId}", jogoId);
                return BadRequest("UsuarioId é obrigatório.");
            }

            await _pedidoService.IniciarCompraAsync(jogoId, usuarioId);

            _logger.LogInformation("Pedido iniciado para o jogo {JogoId} pelo usuário {UsuarioId}", jogoId, usuarioId);

            return Accepted();
        }

        [Authorize]
        [HttpGet("usuario/{usuarioId:guid}")]
        [ProducesResponseType(typeof(List<PedidoResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ObterPedidosPorUsuario(Guid usuarioId)
        {
            var pedidos = await _pedidoService.ObterPorUsuarioIdAsync(usuarioId);
            if (!pedidos.Any())
            {
                _logger.LogInformation("Nenhum pedido encontrado para o usuário {UsuarioId}", usuarioId);
                return NoContent();
            }

            _logger.LogInformation("Retornados {Quantidade} pedidos para o usuário {UsuarioId}", pedidos.Count(), usuarioId);

            return Ok(pedidos);
        }
    }
}