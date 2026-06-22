using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Domain.Entities
{
    public class JogoEntity : Entity
    {
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public JogoGeneroEnum Genero { get; set; }
        public decimal Preco { get; set; }
        public decimal? PrecoDesconto { get; set; }
        public DateTime DataLancamento { get; set; }
        public DateTime DataCriacao{get;set;}
        public JogoStatusEnum StatusJogo { get; set; }
        public ICollection<BibliotecaUsuarioEntity> Bibliotecas { get; set; }
        public JogoEntity()
        {
            Bibliotecas = new List<BibliotecaUsuarioEntity>();
        }
    }
}