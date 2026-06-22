using FluentValidation;

namespace FCG.Catalogo.Domain.Entities.Validations
{
    
    public class JogoValidation : AbstractValidator<JogoEntity>
    {
        public JogoValidation()
        {
            RuleFor(j => j.Titulo)
                .NotEmpty().WithMessage("O título do jogo é obrigatório.")
                .MaximumLength(100).WithMessage("O título do jogo deve ter no máximo 100 caracteres.");

            RuleFor(j => j.Descricao)
                .NotEmpty().WithMessage("A descrição do jogo é obrigatória.")
                .MaximumLength(1000).WithMessage("A descrição do jogo deve ter no máximo 1000 caracteres.");
        
            RuleFor(x => x.Genero)
                .IsInEnum().WithMessage("Gênero inválido.");

            RuleFor(j => j.Preco)
                .GreaterThan(0).WithMessage("O preço deve ser maior que zero.");

            RuleFor(x => x.DataLancamento)
                .NotEmpty().WithMessage("Data de lançamento é obrigatória.");            
        }
    }
    
}