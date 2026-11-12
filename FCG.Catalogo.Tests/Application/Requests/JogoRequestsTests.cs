using System.ComponentModel.DataAnnotations;
using FCG.Catalogo.Application.Requests.Jogos;
using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Tests.Application.Requests;

// Estes atributos são a validação que o endpoint de jogos realmente aplica (o JogoValidation do FluentValidation não está registrado).
public class JogoRequestsTests
{
    private static List<ValidationResult> Validar(object request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static bool TemErroEm(List<ValidationResult> erros, string propriedade)
        => erros.Any(e => e.MemberNames.Contains(propriedade));

    private static CriarJogoRequest CriarValido() => new()
    {
        Titulo = "Zelda",
        Descricao = "Aventura épica",
        Genero = JogoGeneroEnum.Aventura,
        Preco = 199.90m,
        DataLancamento = new DateTime(2023, 5, 12)
    };

    [Fact]
    public void Criar_valido_passa()
    {
        Assert.Empty(Validar(CriarValido()));
    }

    [Fact]
    public void Criar_sem_titulo_falha()
    {
        Assert.True(TemErroEm(Validar(CriarValido() with { Titulo = null! }), nameof(CriarJogoRequest.Titulo)));
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(201, true)]
    public void Criar_respeita_o_limite_de_200_caracteres_no_titulo(int tamanho, bool deveFalhar)
    {
        var erros = Validar(CriarValido() with { Titulo = new string('a', tamanho) });

        Assert.Equal(deveFalhar, TemErroEm(erros, nameof(CriarJogoRequest.Titulo)));
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(501, true)]
    public void Criar_respeita_o_limite_de_500_caracteres_na_descricao(int tamanho, bool deveFalhar)
    {
        var erros = Validar(CriarValido() with { Descricao = new string('a', tamanho) });

        Assert.Equal(deveFalhar, TemErroEm(erros, nameof(CriarJogoRequest.Descricao)));
    }

    [Fact]
    public void Criar_com_genero_inexistente_falha()
    {
        var erros = Validar(CriarValido() with { Genero = (JogoGeneroEnum)99 });

        Assert.True(TemErroEm(erros, nameof(CriarJogoRequest.Genero)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Criar_com_preco_zero_ou_negativo_falha(double preco)
    {
        var erros = Validar(CriarValido() with { Preco = (decimal)preco });

        Assert.True(TemErroEm(erros, nameof(CriarJogoRequest.Preco)));
    }

    [Fact]
    public void Criar_com_preco_promocional_zero_falha()
    {
        var erros = Validar(CriarValido() with { PrecoPromocional = 0 });

        Assert.True(TemErroEm(erros, nameof(CriarJogoRequest.PrecoPromocional)));
    }

    [Fact]
    public void Atualizar_sem_nenhum_campo_passa()
    {
        Assert.Empty(Validar(new AtualizarJogoRequest()));
    }

    [Fact]
    public void Atualizar_com_preco_zero_falha()
    {
        Assert.True(TemErroEm(Validar(new AtualizarJogoRequest { Preco = 0 }), nameof(AtualizarJogoRequest.Preco)));
    }

    [Fact]
    public void Atualizar_com_titulo_acima_de_200_caracteres_falha()
    {
        var erros = Validar(new AtualizarJogoRequest { Titulo = new string('a', 201) });

        Assert.True(TemErroEm(erros, nameof(AtualizarJogoRequest.Titulo)));
    }

    [Fact]
    public void Atualizar_com_genero_inexistente_falha()
    {
        var erros = Validar(new AtualizarJogoRequest { Genero = (JogoGeneroEnum)99 });

        Assert.True(TemErroEm(erros, nameof(AtualizarJogoRequest.Genero)));
    }
}
