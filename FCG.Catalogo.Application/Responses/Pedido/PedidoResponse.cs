namespace FCG.Catalogo.Application.Responses.Pedidos
{
    public class PedidoResponse
    {
        public Guid Id { get; set; }
        public Guid JogoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}