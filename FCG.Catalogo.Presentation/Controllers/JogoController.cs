using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Application.Services;
using FCG.Catalogo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;


namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/jogos")]
    public class JogoController : ControllerBase
    {
        private readonly IJogoService _jogoService;

        public JogoController(IJogoService jogoService)
        {
            _jogoService = jogoService;
        }


        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(List<JogoResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task <IActionResult> ObterTodosJogos()
        {
            var response = await _jogoService.ObterTodosAsync();
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(JogoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var response = await _jogoService.ObterJogoPorIdAsync(id);
            if (response == null)
                return NotFound();

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("buscar/{titulo}")]
        [ProducesResponseType(typeof(JogoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ObterPorTitulo(string titulo)
        {
            var response = await _jogoService.ObterJogoPorTituloAsync(titulo);
            if (response == null)
                return NotFound();

            return Ok(response);
        }

        // [Authorize(Roles = "Admin")]
        [AllowAnonymous]
        [HttpPost("cadastrar")]
        [ProducesResponseType(typeof(JogoResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CadastrarJogo(CriarJogoRequest request)
        {
            var response = await _jogoService.CriarJogoAsync(request);
            return CreatedAtAction(nameof(CadastrarJogo), new { id = response.Id }, response);
        }

        // [Authorize(Roles = "Admin")]
        [AllowAnonymous]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(JogoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AtualizarJogo(Guid id, AtualizarJogoRequest request)
        {
            var response = await _jogoService.AtualizarJogoAsync(id, request);
            return Ok(response);
        }

        // [Authorize(Roles = "Admin")]
        [AllowAnonymous]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound) ]
        public async Task<IActionResult> DeletarJogo(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("O título do jogo é obrigatório.");
            
            await _jogoService.DeletarJogoAsync(id);
            return NoContent();
        }
    }
}