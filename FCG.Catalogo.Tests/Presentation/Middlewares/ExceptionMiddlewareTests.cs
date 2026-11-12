using System.Text.Json;
using FCG.Catalogo.Application.Interfaces;
using FCG.Catalogo.Domain.Exceptions;
using FCG.Catalogo.Presentation.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FCG.Catalogo.Tests.Presentation.Middlewares;

public class ExceptionMiddlewareTests
{
    private readonly IErroLogService _erroLog = Substitute.For<IErroLogService>();

    private static DefaultHttpContext NovoContexto()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/jogos/search";
        contexto.Request.Method = "GET";
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static JsonElement LerCorpo(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        return JsonDocument.Parse(contexto.Response.Body).RootElement.Clone();
    }

    private async Task<DefaultHttpContext> Executar(Exception? excecao)
    {
        var contexto = NovoContexto();
        var middleware = new ExceptionMiddleware(
            _ => excecao is null ? Task.CompletedTask : throw excecao,
            NullLogger<ExceptionMiddleware>.Instance,
            _erroLog);

        await middleware.InvokeAsync(contexto);
        return contexto;
    }

    [Fact]
    public async Task Sem_excecao_a_requisicao_segue_sem_alteracao()
    {
        var contexto = await Executar(null);

        Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
        await _erroLog.DidNotReceiveWithAnyArgs().RegistrarAsync(default!, default!, default);
    }

    [Fact]
    public async Task Busca_indisponivel_vira_503_com_a_mensagem_e_o_caminho()
    {
        var contexto = await Executar(new BuscaIndisponivelException("O serviço de busca está indisponível no momento."));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, contexto.Response.StatusCode);
        var corpo = LerCorpo(contexto);
        Assert.Equal(503, corpo.GetProperty("status").GetInt32());
        Assert.Equal("O serviço de busca está indisponível no momento.", corpo.GetProperty("error").GetString());
        Assert.Equal("/api/jogos/search", corpo.GetProperty("path").GetString());
    }

    [Theory]
    [MemberData(nameof(ExcecoesEStatus))]
    public async Task Excecao_de_dominio_vira_o_status_correspondente(Exception excecao, int statusEsperado)
    {
        var contexto = await Executar(excecao);

        Assert.Equal(statusEsperado, contexto.Response.StatusCode);
        Assert.Equal(excecao.Message, LerCorpo(contexto).GetProperty("error").GetString());
    }

    public static IEnumerable<object[]> ExcecoesEStatus()
    {
        yield return new object[] { new NotFoundException("Jogo não encontrado."), StatusCodes.Status404NotFound };
        yield return new object[] { new ConflictException("Já existe um jogo com este título."), StatusCodes.Status409Conflict };
        yield return new object[] { new UnauthorizedException("Sem permissão."), StatusCodes.Status401Unauthorized };
    }

    [Fact]
    public async Task Excecao_inesperada_vira_500_sem_vazar_a_mensagem_interna()
    {
        var contexto = await Executar(new InvalidOperationException("Server=interno;Password=segredo"));

        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.Equal("Ocorreu um erro interno.", LerCorpo(contexto).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Toda_excecao_e_registrada_no_log_de_erros()
    {
        var excecao = new BuscaIndisponivelException("fora do ar");

        await Executar(excecao);

        await _erroLog.Received(1).RegistrarAsync(excecao, "/api/jogos/search", Arg.Any<object?>());
    }
}
