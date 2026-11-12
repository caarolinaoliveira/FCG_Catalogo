using FCG.Catalogo.Domain.Entities;
using FCG.Catalogo.Domain.Entities.Validations;
using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Tests.Domain;

public class JogoValidationTests
{
    private readonly JogoValidation _validator = new();

    private static JogoEntity JogoValido() => new()
    {
        Titulo = "Zelda",
        Descricao = "Aventura épica",
        Genero = JogoGeneroEnum.Aventura,
        Preco = 199.90m,
        DataLancamento = new DateTime(2023, 5, 12)
    };

    private bool TemErroEm(JogoEntity jogo, string propriedade)
        => _validator.Validate(jogo).Errors.Any(e => e.PropertyName == propriedade);

    [Fact]
    public void Jogo_valido_passa()
    {
        Assert.True(_validator.Validate(JogoValido()).IsValid);
    }

    [Fact]
    public void Titulo_vazio_falha()
    {
        var jogo = JogoValido();
        jogo.Titulo = "";

        Assert.True(TemErroEm(jogo, nameof(JogoEntity.Titulo)));
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(101, true)]
    public void Titulo_respeita_o_limite_de_100_caracteres(int tamanho, bool deveFalhar)
    {
        var jogo = JogoValido();
        jogo.Titulo = new string('a', tamanho);

        Assert.Equal(deveFalhar, TemErroEm(jogo, nameof(JogoEntity.Titulo)));
    }

    [Fact]
    public void Descricao_vazia_falha()
    {
        var jogo = JogoValido();
        jogo.Descricao = "";

        Assert.True(TemErroEm(jogo, nameof(JogoEntity.Descricao)));
    }

    [Theory]
    [InlineData(1000, false)]
    [InlineData(1001, true)]
    public void Descricao_respeita_o_limite_de_1000_caracteres(int tamanho, bool deveFalhar)
    {
        var jogo = JogoValido();
        jogo.Descricao = new string('a', tamanho);

        Assert.Equal(deveFalhar, TemErroEm(jogo, nameof(JogoEntity.Descricao)));
    }

    [Fact]
    public void Genero_fora_do_enum_falha()
    {
        var jogo = JogoValido();
        jogo.Genero = (JogoGeneroEnum)99;

        Assert.True(TemErroEm(jogo, nameof(JogoEntity.Genero)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Preco_zero_ou_negativo_falha(double preco)
    {
        var jogo = JogoValido();
        jogo.Preco = (decimal)preco;

        Assert.True(TemErroEm(jogo, nameof(JogoEntity.Preco)));
    }

    [Fact]
    public void Data_de_lancamento_nao_informada_falha()
    {
        var jogo = JogoValido();
        jogo.DataLancamento = default;

        Assert.True(TemErroEm(jogo, nameof(JogoEntity.DataLancamento)));
    }
}
