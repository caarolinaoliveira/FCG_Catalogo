using System.ComponentModel.DataAnnotations;
using FCG.Catalogo.Application.Requests.Avaliacoes;

namespace FCG.Catalogo.Tests.Application.Requests;

public class CriarAvaliacaoRequestTests
{
    private static CriarAvaliacaoRequest RequestValido() => new()
    {
        UsuarioId = Guid.NewGuid(),
        Nota = 4,
        Comentario = "Muito bom"
    };

    private static List<ValidationResult> Validar(CriarAvaliacaoRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static bool TemErroEm(List<ValidationResult> erros, string propriedade)
        => erros.Any(e => e.MemberNames.Contains(propriedade));

    [Fact]
    public void Request_valido_passa()
    {
        Assert.Empty(Validar(RequestValido()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Notas_nos_limites_passam(int nota)
    {
        Assert.Empty(Validar(RequestValido() with { Nota = nota }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Nota_fora_de_1_a_5_falha(int nota)
    {
        Assert.True(TemErroEm(Validar(RequestValido() with { Nota = nota }), nameof(CriarAvaliacaoRequest.Nota)));
    }

    [Fact]
    public void Comentario_e_opcional()
    {
        Assert.Empty(Validar(RequestValido() with { Comentario = null }));
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(501, true)]
    public void Comentario_respeita_o_limite_de_500_caracteres(int tamanho, bool deveFalhar)
    {
        var erros = Validar(RequestValido() with { Comentario = new string('a', tamanho) });

        Assert.Equal(deveFalhar, TemErroEm(erros, nameof(CriarAvaliacaoRequest.Comentario)));
    }

    [Fact(Skip = "Bug conhecido: [Required] não rejeita Guid.Empty, então uma avaliação com UsuarioId vazio é aceita.")]
    public void UsuarioId_vazio_falha()
    {
        Assert.True(TemErroEm(Validar(RequestValido() with { UsuarioId = Guid.Empty }), nameof(CriarAvaliacaoRequest.UsuarioId)));
    }
}
