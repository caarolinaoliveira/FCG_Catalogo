namespace FCG.Catalogo.Application.Responses.Jogos
{
    public sealed record BuscaJogosResponse
    {
        public int Pagina { get; init; }
        public int Tamanho { get; init; }
        public long Total { get; init; }
        public IReadOnlyList<JogoBuscaItemResponse> Itens { get; init; } = Array.Empty<JogoBuscaItemResponse>();
    }

    public sealed record JogoBuscaItemResponse
    {
        public Guid Id { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public string? Genero { get; init; }
        public decimal Preco { get; init; }
        public decimal? PrecoPromocional { get; init; }
        public DateTime DataLancamento { get; init; }
        public double Score { get; init; }
    }
}
