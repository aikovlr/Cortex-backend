using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarTarefaValidator : AbstractValidator<AtualizarTarefaDTO>
    {
        public AtualizarTarefaValidator()
        {
            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("O título da tarefa é obrigatório.")
                .MaximumLength(255).WithMessage("O título da tarefa não pode exceder 255 caracteres.");

            RuleFor(x => x.DataVencimento)
                .NotEmpty().WithMessage("A data de vencimento da tarefa é obrigatória.");

            RuleFor(x => x.Prioridade)
                .IsInEnum().WithMessage("A prioridade informada é inválida.");

            RuleFor(x => x.Pontuacao)
                .GreaterThanOrEqualTo(0).WithMessage("A pontuação deve ser maior ou igual a zero.")
                .When(x => x.Pontuacao.HasValue);
        }
    }
}
