using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarTarefaValidator : AbstractValidator<CriarTarefaDTO>
    {
        public CriarTarefaValidator()
        {
            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("O título da tarefa é obrigatório.")
                .MaximumLength(255).WithMessage("O título da tarefa não pode exceder 255 caracteres.");

            RuleFor(x => x.DataVencimento)
                .NotEmpty().WithMessage("A data de vencimento da tarefa é obrigatória.")
                .Must(data => data >= DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("A data de vencimento da tarefa não pode ser anterior à data e hora atual.");

            RuleFor(x => x.Prioridade)
                .IsInEnum().WithMessage("A prioridade informada é inválida.");

            RuleFor(x => x.Pontuacao)
                .GreaterThanOrEqualTo(0).WithMessage("A pontuação deve ser maior ou igual a zero.")
                .When(x => x.Pontuacao.HasValue);
        }
    }
}
