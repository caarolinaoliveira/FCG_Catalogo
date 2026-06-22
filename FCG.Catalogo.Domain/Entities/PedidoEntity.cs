using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Domain.Entities
{
    public class PedidoEntity : Entity
    {
        public Guid UsuarioId { get; set; }
        public Guid JogoId { get; set; }
        public decimal Preco { get; set; }
        public PedidoStatusEnum Status { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
        public JogoEntity Jogo { get; set; } = null!;

        public void AtualizarStatus(PedidoStatusEnum novoStatus)
        {
            Status = novoStatus;
            AtualizadoEm = DateTime.UtcNow;
        }
    }
}