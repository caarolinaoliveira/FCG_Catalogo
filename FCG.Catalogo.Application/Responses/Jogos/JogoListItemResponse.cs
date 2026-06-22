using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Application.Responses.Jogos
{
    public sealed record JogoListItemResponse
    {
        public Guid Id { get; init; }
        public string Titulo { get; init; }
        public decimal Preco { get; init; }
        public decimal? PrecoPromocional { get; init; }
        public string? Genero { get; init; }
        public string StatusJogo { get; init; }
    }
}