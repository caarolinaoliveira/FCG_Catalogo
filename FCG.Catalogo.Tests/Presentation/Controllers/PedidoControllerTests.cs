using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Responses.Pedidos;
using FCG.Catalogo.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FCG.Catalogo.Tests.Presentation.Controllers;

// O pedido não tem validator: a única validação de entrada está no controller.
public class PedidoControllerTests
{
    private readonly IPedidoService _service = Substitute.For<IPedidoService>();
    private readonly PedidoController _controller;

    public PedidoControllerTests()
    {
        _controller = new PedidoController(_service, NullLogger<PedidoController>.Instance);
    }

    [Fact]
    public async Task Pedido_com_usuario_vazio_devolve_400_sem_iniciar_a_compra()
    {
        var resultado = await _controller.RealizarPedido(Guid.NewGuid(), Guid.Empty);

        Assert.IsType<BadRequestObjectResult>(resultado);
        await _service.DidNotReceiveWithAnyArgs().IniciarCompraAsync(default, default);
    }

    [Fact]
    public async Task Pedido_valido_devolve_202_e_inicia_a_compra()
    {
        var jogoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var resultado = await _controller.RealizarPedido(jogoId, usuarioId);

        Assert.IsType<AcceptedResult>(resultado);
        await _service.Received(1).IniciarCompraAsync(jogoId, usuarioId);
    }

    [Fact]
    public async Task Usuario_sem_pedidos_recebe_204()
    {
        _service.ObterPorUsuarioIdAsync(Arg.Any<Guid>()).Returns(new List<PedidoResponse>());

        Assert.IsType<NoContentResult>(await _controller.ObterPedidosPorUsuario(Guid.NewGuid()));
    }

    [Fact]
    public async Task Usuario_com_pedidos_recebe_200_com_a_lista()
    {
        var pedidos = new List<PedidoResponse> { new() { Id = Guid.NewGuid(), Titulo = "Zelda" } };
        _service.ObterPorUsuarioIdAsync(Arg.Any<Guid>()).Returns(pedidos);

        var resultado = await _controller.ObterPedidosPorUsuario(Guid.NewGuid());

        Assert.Same(pedidos, Assert.IsType<OkObjectResult>(resultado).Value);
    }
}
