using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarEquipeValidator : AbstractValidator<AtualizarEquipeDTO>
    {
        public AtualizarEquipeValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da equipe é obrigatório.")
                .MaximumLength(255).WithMessage("O nome da equipe não pode exceder 255 caracteres.");
        }
    }
}
