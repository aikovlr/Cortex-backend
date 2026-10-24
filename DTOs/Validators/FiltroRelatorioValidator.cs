using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class FiltroRelatorioValidator : AbstractValidator<FiltroRelatorioDTO>
    {
        public FiltroRelatorioValidator()
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

            When(x => x.EquipeId.HasValue, () =>
            {
                RuleFor(x => x.EquipeId!.Value)
                    .GreaterThan(0).WithMessage("O identificador da equipe no filtro deve ser válido.");
            });

            When(x => x.ResponsavelId.HasValue, () =>
            {
                RuleFor(x => x.ResponsavelId!.Value)
                    .GreaterThan(0).WithMessage("O identificador do responsável no filtro deve ser válido.");
            });
        }
    }
}
