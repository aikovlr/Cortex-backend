using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtribuirEquipeMetaValidator : AbstractValidator<AtribuirEquipeMetaDTO>
    {
        public AtribuirEquipeMetaValidator()
        {
            RuleFor(x => x.EquipeId)
                .GreaterThan(0)
                .WithMessage("O ID da equipe é obrigatório e deve ser maior que zero.");
        }
    }
}

