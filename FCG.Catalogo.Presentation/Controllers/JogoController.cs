using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Application.Services;
using FCG.Catalogo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FCG.Catalogo.Presentation.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.Extensions.Logging; 


namespace FCG.Catalogo.Presentation.Controllers
{
    [Route("api/jogos")]
    public class JogoController : ControllerBase
    {
        private readonly IJogoService _jogoService;
        private readonly ILogger<JogoController> _logger;

        public JogoController(IJogoService jogoService, ILogger<JogoController> logger)
        {
            _jogoService = jogoService;
            _logger = logger;
        }


        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(List<JogoResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task <IActionResult> ObterTodosJogos()
        {
            var response = await _jogoService.ObterTodosAsync();

            _logger.LogInformation("Consulta de todos os jogos retornou {Quantidade} registros", response?.Count() ?? 0);

            return Ok(response);
        }

        [AllowAnonymous] // público, apesar da FallbackPolicy que exige autenticação nas demais rotas
        [HttpGet("search")]
        [ProducesResponseType(typeof(BuscaJogosResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable)]
        public async Task<IActionResult> Buscar(
            [FromQuery] string? q,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanho = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("O parâmetro 'q' (texto da busca) é obrigatório.");
            if (pagina < 1)
                return BadRequest("O parâmetro 'pagina' deve ser maior ou igual a 1.");
            if (tamanho is < 1 or > 50)
                return BadRequest("O parâmetro 'tamanho' deve estar entre 1 e 50.");

            var response = await _jogoService.BuscarAsync(q.Trim(), pagina, tamanho, cancellationToken);

            _logger.LogInformation("Busca por '{Termo}' retornou {Quantidade} de {Total} resultado(s)", q, response.Itens.Count, response.Total);

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
            {
                _logger.LogWarning("Jogo não encontrado para o Id {Id}", id);
                return NotFound();
            }

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
            {
                _logger.LogWarning("Jogo não encontrado para o título {Titulo}", titulo);
                return NotFound();
            }

            return Ok(response);
        }

        [Authorize(Roles = Papeis.Admin)]
        [HttpPost("cadastrar")]
        [ProducesResponseType(typeof(JogoResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CadastrarJogo([FromBody] CriarJogoRequest request)
        {
            var response = await _jogoService.CriarJogoAsync(request);

            _logger.LogInformation("Jogo {Id} cadastrado com sucesso ({Titulo})", response.Id, response.Titulo);

            return CreatedAtAction(nameof(CadastrarJogo), new { id = response.Id }, response);
        }

        [Authorize(Roles = Papeis.Admin)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(JogoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AtualizarJogo(Guid id, [FromBody] AtualizarJogoRequest request)
        {
            var response = await _jogoService.AtualizarJogoAsync(id, request);

            _logger.LogInformation("Jogo {Id} atualizado com sucesso", id);

            return Ok(response);
        }

        [Authorize(Roles = Papeis.Admin)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound) ]
        public async Task<IActionResult> DeletarJogo(Guid id)
        {
            if (id == Guid.Empty)
            {
                _logger.LogWarning("Tentativa de exclusão com Id vazio");
                return BadRequest("O título do jogo é obrigatório.");
            }

            await _jogoService.DeletarJogoAsync(id);

            _logger.LogInformation("Jogo {Id} excluído com sucesso", id);

            return NoContent();
        }
    }
}