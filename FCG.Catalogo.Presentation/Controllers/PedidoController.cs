using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/pedidos")]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;

        public PedidoController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        [AllowAnonymous]
        [HttpPost("{jogoId:guid}")]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> RealizarPedido(Guid jogoId, [FromQuery] Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
                return BadRequest("UsuarioId é obrigatório.");

            await _pedidoService.IniciarCompraAsync(jogoId, usuarioId);
            return Accepted();
        }

        [AllowAnonymous]
        [HttpGet("usuario/{usuarioId:guid}")]
        [ProducesResponseType(typeof(List<PedidoResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ObterPedidosPorUsuario(Guid usuarioId)
        {
            var pedidos = await _pedidoService.ObterPorUsuarioIdAsync(usuarioId);
            if (!pedidos.Any())
                return NoContent();

            return Ok(pedidos);
        }
    }
}