using FCG.Catalogo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.Extensions.Logging;

namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/biblioteca")]
    public class BibliotecaController : ControllerBase
    {
        private readonly IBibliotecaService _bibliotecaService;
        private readonly ILogger<BibliotecaController> _logger;

        public BibliotecaController(IBibliotecaService bibliotecaService, ILogger<BibliotecaController> logger)
        {
            _bibliotecaService = bibliotecaService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("{usuarioId:guid}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ObterBiblioteca(Guid usuarioId)
        {
            var response = await _bibliotecaService.ObterBibliotecaAsync(usuarioId);
            if (response == null || !response.Any())
            {
                _logger.LogInformation("Biblioteca vazia para o usuário {UsuarioId}", usuarioId);
                return NoContent();
            }

            _logger.LogInformation("Retornados {Quantidade} itens da biblioteca do usuário {UsuarioId}", response.Count(), usuarioId);

            return Ok(response);
        }
    }
}