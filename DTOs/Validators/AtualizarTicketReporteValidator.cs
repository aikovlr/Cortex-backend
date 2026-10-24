using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarTicketReporteValidator : AbstractValidator<AtualizarTicketReporteDTO>
    {
        public AtualizarTicketReporteValidator()
        {
            RuleFor(x => x.Ativo)
                .NotNull().WithMessage("O status do ticket de reporte é obrigatório.");
        }
    }
}
