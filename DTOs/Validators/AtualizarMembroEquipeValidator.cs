using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarMembroEquipeValidator : AbstractValidator<AtualizarMembroEquipeDTO>
    {
        public AtualizarMembroEquipeValidator()
        {
            RuleFor(x => x.Cargo)
                .IsInEnum().WithMessage("O cargo informado é inválido.");
        }
    }
}
