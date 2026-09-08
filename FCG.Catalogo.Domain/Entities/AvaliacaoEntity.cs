namespace FCG.Catalogo.Domain.Entities
{
    public class AvaliacaoEntity : Entity
    {
        public Guid JogoId { get; set; }
        public Guid UsuarioId { get; set; }
        public int Nota { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
    }
}
