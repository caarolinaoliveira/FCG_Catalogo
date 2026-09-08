namespace FCG.Catalogo.Application.Responses.Avaliacoes
{
    public sealed record AvaliacaoResumoResponse
    {
        public double MediaNotas { get; init; }
        public int TotalAvaliacoes { get; init; }
        public List<AvaliacaoResponse> Avaliacoes { get; init; } = new();
    }
}
