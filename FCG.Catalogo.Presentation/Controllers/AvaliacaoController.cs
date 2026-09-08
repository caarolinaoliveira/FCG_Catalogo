using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Requests.Avaliacoes;
using FCG.Catalogo.Application.Responses.Avaliacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/jogos/{jogoId:guid}/avaliacoes")]
    public class AvaliacaoController : ControllerBase
    {
        private readonly IAvaliacaoService _avaliacaoService;

        public AvaliacaoController(IAvaliacaoService avaliacaoService)
        {
            _avaliacaoService = avaliacaoService;
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(typeof(AvaliacaoResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Criar(Guid jogoId, [FromBody] CriarAvaliacaoRequest request)
        {
            var response = await _avaliacaoService.CriarAsync(jogoId, request);
            return CreatedAtAction(nameof(ObterPorJogo), new { jogoId }, response);
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(AvaliacaoResumoResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObterPorJogo(Guid jogoId)
        {
            var response = await _avaliacaoService.ObterPorJogoIdAsync(jogoId);
            return Ok(response);
        }
    }
}
