using FCG.Catalogo.Application.Events;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Application.Services.Pedidos;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Enums;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Domain.Interfaces;
using NSubstitute;

namespace FCG.Catalogo.Tests.Application.Services;

public class PedidoServiceTests
{
    private readonly IJogoRepository _jogos = Substitute.For<IJogoRepository>();
    private readonly IBibliotecaRepository _biblioteca = Substitute.For<IBibliotecaRepository>();
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private readonly PedidoService _service;

    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly JogoEntity _jogo = new() { Titulo = "Zelda", Preco = 199.90m };

    public PedidoServiceTests()
    {
        _service = new PedidoService(_jogos, _biblioteca, _pedidos, _publisher);
        _jogos.ObterPorIdAsync(_jogo.Id).Returns(_jogo);
    }

    [Fact]
    public async Task Compra_cria_pedido_aguardando_pagamento_com_o_preco_do_jogo()
    {
        PedidoEntity? pedido = null;
        await _pedidos.AdicionarAsync(Arg.Do<PedidoEntity>(p => pedido = p));

        await _service.IniciarCompraAsync(_jogo.Id, _usuarioId);

        Assert.NotNull(pedido);
        Assert.Equal(_usuarioId, pedido!.UsuarioId);
        Assert.Equal(_jogo.Id, pedido.JogoId);
        Assert.Equal(199.90m, pedido.Preco);
        Assert.Equal(PedidoStatusEnum.AguardandoPagamento, pedido.Status);
    }

    [Fact]
    public async Task Compra_publica_order_placed_com_usuario_jogo_e_preco()
    {
        OrderPlacedEvent? evento = null;
        await _publisher.PublicarAsync(Arg.Do<OrderPlacedEvent>(e => evento = e), "order.placed");

        await _service.IniciarCompraAsync(_jogo.Id, _usuarioId);

        await _publisher.Received(1).PublicarAsync(Arg.Any<OrderPlacedEvent>(), "order.placed");
        Assert.Equal(_usuarioId, evento!.UserId);
        Assert.Equal(_jogo.Id, evento.GameId);
        Assert.Equal(199.90m, evento.Price);
    }

    [Fact]
    public async Task Compra_grava_o_pedido_antes_de_publicar()
    {
        await _service.IniciarCompraAsync(_jogo.Id, _usuarioId);

        Received.InOrder(() =>
        {
            _pedidos.AdicionarAsync(Arg.Any<PedidoEntity>());
            _publisher.PublicarAsync(Arg.Any<OrderPlacedEvent>(), "order.placed");
        });
    }

    [Fact]
    public async Task Usuario_que_ja_possui_o_jogo_recebe_conflito_sem_pedido_nem_evento()
    {
        _biblioteca.UsuarioPossuiJogoAsync(_usuarioId, _jogo.Id).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.IniciarCompraAsync(_jogo.Id, _usuarioId));

        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!);
        await _publisher.DidNotReceiveWithAnyArgs().PublicarAsync<OrderPlacedEvent>(default!, default!);
    }

    [Fact]
    public async Task Jogo_inexistente_lanca_nao_encontrado()
    {
        _jogos.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.IniciarCompraAsync(Guid.NewGuid(), _usuarioId));
    }

    [Fact]
    public async Task Jogo_inexistente_nao_cria_pedido_nem_publica()
    {
        _jogos.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);

        await Assert.ThrowsAnyAsync<Exception>(() => _service.IniciarCompraAsync(Guid.NewGuid(), _usuarioId));

        await _pedidos.DidNotReceiveWithAnyArgs().AdicionarAsync(default!);
        await _publisher.DidNotReceiveWithAnyArgs().PublicarAsync<OrderPlacedEvent>(default!, default!);
    }

    [Fact]
    public async Task Listagem_de_pedidos_mapeia_titulo_status_e_preco()
    {
        var pedido = new PedidoEntity { UsuarioId = _usuarioId, JogoId = _jogo.Id, Preco = 199.90m, Jogo = _jogo };
        pedido.AtualizarStatus(PedidoStatusEnum.Aprovado);
        _pedidos.ObterPorUsuarioIdAsync(_usuarioId).Returns(new List<PedidoEntity> { pedido });

        var resposta = await _service.ObterPorUsuarioIdAsync(_usuarioId);

        var item = Assert.Single(resposta);
        Assert.Equal("Zelda", item.Titulo);
        Assert.Equal("Aprovado", item.Status);
        Assert.Equal(199.90m, item.Preco);
        Assert.NotNull(item.AtualizadoEm);
    }
}
