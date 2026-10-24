using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarEquipeValidator : AbstractValidator<CriarEquipeDTO>
    {
        public CriarEquipeValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da equipe é obrigatório.")
                .MaximumLength(255).WithMessage("O nome da equipe não pode exceder 255 caracteres.");
        }
    }
}
