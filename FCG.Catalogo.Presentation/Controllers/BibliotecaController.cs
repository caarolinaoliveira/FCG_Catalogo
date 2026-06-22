using FCG.Catalogo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/biblioteca")]
    public class BibliotecaController : ControllerBase
    {
        private readonly IBibliotecaService _bibliotecaService;

        public BibliotecaController(IBibliotecaService bibliotecaService)
        {
            _bibliotecaService = bibliotecaService;
        }

        [AllowAnonymous]
        [HttpGet("{usuarioId:guid}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> ObterBiblioteca(Guid usuarioId)
        {
            var response = await _bibliotecaService.ObterBibliotecaAsync(usuarioId);
            if (response == null || !response.Any())
                return NoContent();

            return Ok(response);
        }
    }
}