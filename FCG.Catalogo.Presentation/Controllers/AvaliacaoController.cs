using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Requests.Avaliacoes;
using FCG.Catalogo.Application.Responses.Avaliacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Logging;

namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/jogos/{jogoId:guid}/avaliacoes")]
    public class AvaliacaoController : ControllerBase
    {
        private readonly IAvaliacaoService _avaliacaoService;
        private readonly ILogger<AvaliacaoController> _logger;

        public AvaliacaoController(IAvaliacaoService avaliacaoService, ILogger<AvaliacaoController> logger)
        {
            _avaliacaoService = avaliacaoService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(typeof(AvaliacaoResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Criar(Guid jogoId, [FromBody] CriarAvaliacaoRequest request)
        {
            var response = await _avaliacaoService.CriarAsync(jogoId, request);

            _logger.LogInformation("Avaliação {Id} criada para o jogo {JogoId}", response.Id, jogoId);

            return CreatedAtAction(nameof(ObterPorJogo), new { jogoId }, response);
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(AvaliacaoResumoResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObterPorJogo(Guid jogoId)
        {
            var response = await _avaliacaoService.ObterPorJogoIdAsync(jogoId);

            _logger.LogInformation("Resumo de avaliações consultado para o jogo {JogoId}", jogoId);

            return Ok(response);
        }
    }
}