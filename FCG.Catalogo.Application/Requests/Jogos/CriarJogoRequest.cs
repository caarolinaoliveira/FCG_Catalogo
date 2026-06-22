using System.ComponentModel.DataAnnotations;
using FCG.Catalogo.Domain.Enums;

namespace FCG.Catalogo.Application.Requests.Jogos
{
    public sealed record CriarJogoRequest
    {
        [Required(ErrorMessage = "Título é obrigatório")]
        [MaxLength(200, ErrorMessage = "Título deve ter no máximo 200 caracteres")]
        public string Titulo { get; init; }

        [MaxLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
        public string Descricao { get; init; }

        [Required(ErrorMessage = "Gênero é obrigatório")]
        [EnumDataType(typeof(JogoGeneroEnum), ErrorMessage = "Gênero inválido")]
        public JogoGeneroEnum Genero { get; init; }

        [Required(ErrorMessage = "Preço é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")]
        public decimal Preco { get; init; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Preço promocional deve ser maior que zero")]
        public decimal? PrecoPromocional { get; init; }

        [Required(ErrorMessage = "Data de lançamento é obrigatória")]
        public DateTime DataLancamento { get; init; }
    }
}