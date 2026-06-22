namespace FCG.Catalogo.Application.Responses.Biblioteca
{
    public class BibliotecaResponse
    {
        public Guid JogoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public decimal PrecoPago { get; set; }
        public DateTime DataAquisicao { get; set; }
    }
}