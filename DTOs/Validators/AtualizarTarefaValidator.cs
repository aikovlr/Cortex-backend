using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarTarefaValidator : AbstractValidator<AtualizarTarefaDTO>
    {
        public AtualizarTarefaValidator()
        {
            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("O título da tarefa não pode ser vazio.")
                .MaximumLength(255).WithMessage("O título da tarefa não pode exceder 255 caracteres.")
                .When(x => x.Titulo != null);

            RuleFor(x => x.DataVencimento)
                .NotEmpty().WithMessage("A data de vencimento informada é inválida.")
                .When(x => x.DataVencimento.HasValue);

            RuleFor(x => x.Prioridade)
                .IsInEnum().WithMessage("A prioridade informada é inválida.")
                .When(x => x.Prioridade.HasValue);

            RuleFor(x => x.Pontuacao)
                .GreaterThanOrEqualTo(0).WithMessage("A pontuação deve ser maior ou igual a zero.")
                .When(x => x.Pontuacao.HasValue);
        }
    }
}
