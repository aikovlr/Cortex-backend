using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarResponsavelTarefaValidator : AbstractValidator<CriarResponsavelTarefaDTO>
    {
        public CriarResponsavelTarefaValidator()
        {
            RuleFor(x => x.TarefaId)
                .GreaterThan(0).WithMessage("O identificador da tarefa deve ser válido.");

            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("O identificador do usuário deve ser válido.");
        }
    }
}
