using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class FiltroTarefasValidator : AbstractValidator<FiltroTarefasDTO>
    {
        public FiltroTarefasValidator()
        {
            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status!.Value)
                    .IsInEnum().WithMessage("O status fornecido no filtro é inválido.");
            });

            When(x => x.Prioridade.HasValue, () =>
            {
                RuleFor(x => x.Prioridade!.Value)
                    .IsInEnum().WithMessage("A prioridade fornecida no filtro é inválida.");
            });

            When(x => x.DataInicio.HasValue && x.DataFim.HasValue, () =>
            {
                RuleFor(x => x.DataInicio!.Value)
                    .LessThanOrEqualTo(x => x.DataFim!.Value)
                    .WithMessage("A data inicial do filtro não pode ser posterior à data final.");
            });

            When(x => !string.IsNullOrEmpty(x.TermoBusca), () =>
            {
                RuleFor(x => x.TermoBusca!)
                    .MaximumLength(100).WithMessage("O termo de busca não pode exceder 100 caracteres.");
            });
        }
    }
}
