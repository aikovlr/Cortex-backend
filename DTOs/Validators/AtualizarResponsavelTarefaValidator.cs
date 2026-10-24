using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarResponsavelTarefaValidator : AbstractValidator<AtualizarResponsavelTarefaDTO>
    {
        public AtualizarResponsavelTarefaValidator()
        {
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("O identificador do usuário responsável deve ser válido.");
        }
    }
}
