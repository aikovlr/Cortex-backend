using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarMetaValidator : AbstractValidator<AtualizarMetaDTO>
    {
        public AtualizarMetaValidator()
        {
            When(x => !string.IsNullOrEmpty(x.Nome), () =>
            {
                RuleFor(x => x.Nome!)
                    .MaximumLength(255).WithMessage("O nome da meta não pode exceder 255 caracteres.");
            });

            RuleFor(x => x.DataFechamento)
                .Must(data => !data.HasValue || data.Value <= DateTime.UtcNow.AddMonths(12))
                .WithMessage("A data de término da meta não pode exceder 12 meses a partir da data atual.");

            When(x => x.TarefaId > 0, () =>
            {
                RuleFor(x => x.TarefaId)
                    .GreaterThan(0).WithMessage("O identificador da tarefa vinculada deve ser válido.");
            });

            When(x => x.TarefaIds != null, () =>
            {
                RuleFor(x => x.TarefaIds)
                    .Must(ids => ids != null && ids.Any(id => id > 0))
                    .WithMessage("A meta deve estar vinculada a pelo menos uma tarefa válida.");
            });
        }
    }
}
