using System.Text.Json.Serialization;
using FCG.Catalogo.Domain.Entities;

namespace FCG.Catalogo.Infrastructure.Search
{
    public sealed class JogoDocumento
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("titulo")] public string Titulo { get; set; } = string.Empty;
        [JsonPropertyName("descricao")] public string Descricao { get; set; } = string.Empty;
        [JsonPropertyName("genero")] public string Genero { get; set; } = string.Empty;
        [JsonPropertyName("preco")] public decimal Preco { get; set; }
        [JsonPropertyName("precoPromocional")] public decimal? PrecoPromocional { get; set; }
        [JsonPropertyName("dataLancamento")] public DateTime DataLancamento { get; set; }

        public static JogoDocumento De(JogoEntity jogo) => new()
        {
            Id = jogo.Id,
            Titulo = jogo.Titulo ?? string.Empty,
            Descricao = jogo.Descricao ?? string.Empty,
            Genero = jogo.Genero.ToString(),
            Preco = jogo.Preco,
            PrecoPromocional = jogo.PrecoDesconto,
            DataLancamento = jogo.DataLancamento
        };
    }
}
