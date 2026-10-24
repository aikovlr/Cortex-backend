using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarRespostaTarefaValidator : AbstractValidator<CriarRespostaTarefaDTO>
    {
        public CriarRespostaTarefaValidator()
        {
            RuleFor(x => x.TarefaId)
                .GreaterThan(0).WithMessage("O identificador da tarefa vinculada à resposta deve ser válido.");
        }
    }
}
