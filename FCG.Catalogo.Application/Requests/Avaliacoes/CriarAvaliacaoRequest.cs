using System.ComponentModel.DataAnnotations;

namespace FCG.Catalogo.Application.Requests.Avaliacoes
{
    public sealed record CriarAvaliacaoRequest
    {
        [Required(ErrorMessage = "UsuarioId é obrigatório")]
        public Guid UsuarioId { get; init; }

        [Required(ErrorMessage = "Nota é obrigatória")]
        [Range(1, 5, ErrorMessage = "Nota deve ser entre 1 e 5")]
        public int Nota { get; init; }

        [MaxLength(500, ErrorMessage = "Comentário deve ter no máximo 500 caracteres")]
        public string? Comentario { get; init; }
    }
}
