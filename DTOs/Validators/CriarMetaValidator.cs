using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarMetaValidator : AbstractValidator<CriarMetaDTO>
    {
        public CriarMetaValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da meta é obrigatório.")
                .MaximumLength(255).WithMessage("O nome da meta não pode exceder 255 caracteres.");

            RuleFor(x => x.TarefaId)
                .GreaterThan(0).WithMessage("A meta deve estar vinculada a uma tarefa válida.");

            RuleFor(x => x.DataFechamento)
                .Must(data => !data.HasValue || data.Value <= DateTime.UtcNow.AddMonths(12))
                .WithMessage("A data de término da meta não pode exceder 12 meses a partir da data de criação (RN15).");
        }
    }
}
