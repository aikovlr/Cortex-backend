using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarSugestaoValidator : AbstractValidator<CriarSugestaoDTO>
    {
        public CriarSugestaoValidator()
        {
            RuleFor(x => x.Descricao)
                .NotEmpty().WithMessage("A descrição da sugestão é obrigatória.")
                .MaximumLength(1000).WithMessage("A descrição da sugestão não pode exceder 1000 caracteres.");

            RuleFor(x => x.TarefaId)
                .GreaterThan(0).WithMessage("O identificador da tarefa deve ser válido.");
        }
    }
}
