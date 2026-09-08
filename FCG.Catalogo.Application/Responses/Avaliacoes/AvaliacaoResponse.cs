namespace FCG.Catalogo.Application.Responses.Avaliacoes
{
    public sealed record AvaliacaoResponse
    {
        public Guid Id { get; init; }
        public Guid JogoId { get; init; }
        public Guid UsuarioId { get; init; }
        public int Nota { get; init; }
        public string Comentario { get; init; } = string.Empty;
        public DateTime CriadoEm { get; init; }
    }
}
