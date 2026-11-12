using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Application.Responses.Jogos;
using FCG.Catalogo.Domain.Enums;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Presentation.Controllers;
using FCG.Catalogo.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FCG.Catalogo.Tests.Presentation.Controllers;

public class JogoControllerTests
{
    private readonly IJogoService _service = Substitute.For<IJogoService>();
    private readonly JogoController _controller;

    public JogoControllerTests()
    {
        _controller = new JogoController(_service, NullLogger<JogoController>.Instance);
    }

    private void BuscaRetorna(BuscaJogosResponse resposta)
        => _service.BuscarAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(resposta);

    public class Busca : JogoControllerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Termo_vazio_devolve_400_sem_buscar(string? q)
        {
            var resultado = await _controller.Buscar(q);

            Assert.IsType<BadRequestObjectResult>(resultado);
            await _service.DidNotReceiveWithAnyArgs().BuscarAsync(default!, default, default, default);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(51)]
        [InlineData(1000)]
        public async Task Tamanho_fora_de_1_a_50_devolve_400_sem_buscar(int tamanho)
        {
            var resultado = await _controller.Buscar("zelda", tamanho: tamanho);

            var erro = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Contains("tamanho", erro.Value!.ToString());
            await _service.DidNotReceiveWithAnyArgs().BuscarAsync(default!, default, default, default);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Pagina_menor_que_1_devolve_400_sem_buscar(int pagina)
        {
            var resultado = await _controller.Buscar("zelda", pagina: pagina);

            Assert.IsType<BadRequestObjectResult>(resultado);
            await _service.DidNotReceiveWithAnyArgs().BuscarAsync(default!, default, default, default);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        public async Task Tamanho_nos_limites_e_aceito(int tamanho)
        {
            BuscaRetorna(new BuscaJogosResponse { Pagina = 1, Tamanho = tamanho });

            var resultado = await _controller.Buscar("zelda", tamanho: tamanho);

            Assert.IsType<OkObjectResult>(resultado);
        }

        [Fact]
        public async Task Busca_valida_remove_espacos_do_termo_e_devolve_200_com_o_resultado()
        {
            var esperado = new BuscaJogosResponse { Pagina = 2, Tamanho = 5, Total = 1 };
            using var cts = new CancellationTokenSource();
            _service.BuscarAsync("zelda", 2, 5, cts.Token).Returns(esperado);

            var resultado = await _controller.Buscar("  zelda  ", pagina: 2, tamanho: 5, cancellationToken: cts.Token);

            Assert.Same(esperado, Assert.IsType<OkObjectResult>(resultado).Value);
        }

        [Fact]
        public async Task Busca_indisponivel_vira_503_pelo_middleware()
        {
            _service.BuscarAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new BuscaIndisponivelException("O serviço de busca está indisponível no momento."));
            var contexto = new DefaultHttpContext();
            contexto.Response.Body = new MemoryStream();
            var middleware = new ExceptionMiddleware(
                _ => _controller.Buscar("zelda"),
                NullLogger<ExceptionMiddleware>.Instance,
                Substitute.For<IErroLogService>());

            await middleware.InvokeAsync(contexto);

            Assert.Equal(StatusCodes.Status503ServiceUnavailable, contexto.Response.StatusCode);
        }
    }

    public class Crud : JogoControllerTests
    {
        [Fact]
        public async Task Cadastrar_devolve_201_com_o_jogo_criado()
        {
            var criado = new JogoResponse { Id = Guid.NewGuid(), Titulo = "Zelda" };
            _service.CriarJogoAsync(Arg.Any<CriarJogoRequest>()).Returns(criado);

            var resultado = await _controller.CadastrarJogo(new CriarJogoRequest { Titulo = "Zelda", Genero = JogoGeneroEnum.Aventura });

            var created = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Same(criado, created.Value);
        }

        [Fact]
        public async Task Excluir_com_id_vazio_devolve_400_sem_chamar_o_servico()
        {
            var resultado = await _controller.DeletarJogo(Guid.Empty);

            Assert.IsType<BadRequestObjectResult>(resultado);
            await _service.DidNotReceiveWithAnyArgs().DeletarJogoAsync(default);
        }

        [Fact]
        public async Task Excluir_devolve_204()
        {
            var id = Guid.NewGuid();

            var resultado = await _controller.DeletarJogo(id);

            Assert.IsType<NoContentResult>(resultado);
            await _service.Received(1).DeletarJogoAsync(id);
        }
    }
}
