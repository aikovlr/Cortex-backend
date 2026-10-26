using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarTicketValidator : AbstractValidator<CriarTicketReporteDTO>
    {
        public CriarTicketValidator()
        {
            RuleFor(x => x.Descricao)
                .NotEmpty().WithMessage("A descrição do ticket é obrigatória.")
                .MaximumLength(1000).WithMessage("A descrição do ticket não pode exceder 1000 caracteres.");
        }
    }
}
