using FCG.Catalogo.Application.Requests.Avaliacoes;
using FCG.Catalogo.Application.Services.Avaliacoes;
using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Domain.Interfaces;
using NSubstitute;

namespace FCG.Catalogo.Tests.Application.Services;

public class AvaliacaoServiceTests
{
    private readonly IAvaliacaoRepository _avaliacoes = Substitute.For<IAvaliacaoRepository>();
    private readonly IJogoRepository _jogos = Substitute.For<IJogoRepository>();
    private readonly AvaliacaoService _service;

    public AvaliacaoServiceTests()
    {
        _service = new AvaliacaoService(_avaliacoes, _jogos);
    }

    [Fact]
    public async Task Criar_salva_a_avaliacao_e_devolve_os_dados()
    {
        var jogoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        _jogos.ObterPorIdAsync(jogoId).Returns(new JogoEntity());
        AvaliacaoEntity? salva = null;
        await _avaliacoes.AdicionarAsync(Arg.Do<AvaliacaoEntity>(a => salva = a));

        var resposta = await _service.CriarAsync(jogoId, new CriarAvaliacaoRequest { UsuarioId = usuarioId, Nota = 5, Comentario = "Ótimo" });

        Assert.Equal(jogoId, salva!.JogoId);
        Assert.Equal(usuarioId, salva.UsuarioId);
        Assert.Equal(5, resposta.Nota);
        Assert.Equal("Ótimo", resposta.Comentario);
    }

    [Fact]
    public async Task Criar_sem_comentario_grava_texto_vazio()
    {
        var jogoId = Guid.NewGuid();
        _jogos.ObterPorIdAsync(jogoId).Returns(new JogoEntity());

        var resposta = await _service.CriarAsync(jogoId, new CriarAvaliacaoRequest { UsuarioId = Guid.NewGuid(), Nota = 3, Comentario = null });

        Assert.Equal(string.Empty, resposta.Comentario);
    }

    [Fact]
    public async Task Criar_para_jogo_inexistente_lanca_nao_encontrado_sem_salvar()
    {
        _jogos.ObterPorIdAsync(Arg.Any<Guid>()).Returns((JogoEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CriarAsync(Guid.NewGuid(), new CriarAvaliacaoRequest { UsuarioId = Guid.NewGuid(), Nota = 4 }));

        await _avaliacoes.DidNotReceiveWithAnyArgs().AdicionarAsync(default!);
    }

    [Fact]
    public async Task Resumo_calcula_media_e_total()
    {
        var jogoId = Guid.NewGuid();
        _avaliacoes.ObterPorJogoIdAsync(jogoId).Returns(new List<AvaliacaoEntity>
        {
            new() { JogoId = jogoId, Nota = 5 },
            new() { JogoId = jogoId, Nota = 4 },
            new() { JogoId = jogoId, Nota = 3 }
        });

        var resumo = await _service.ObterPorJogoIdAsync(jogoId);

        Assert.Equal(4, resumo.MediaNotas);
        Assert.Equal(3, resumo.TotalAvaliacoes);
        Assert.Equal(3, resumo.Avaliacoes.Count());
    }

    [Fact]
    public async Task Resumo_sem_avaliacoes_tem_media_zero()
    {
        _avaliacoes.ObterPorJogoIdAsync(Arg.Any<Guid>()).Returns(new List<AvaliacaoEntity>());

        var resumo = await _service.ObterPorJogoIdAsync(Guid.NewGuid());

        Assert.Equal(0, resumo.MediaNotas);
        Assert.Equal(0, resumo.TotalAvaliacoes);
    }
}
