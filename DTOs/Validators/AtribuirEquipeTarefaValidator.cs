using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtribuirEquipeTarefaValidator : AbstractValidator<AtribuirEquipeTarefaDTO>
    {
        public AtribuirEquipeTarefaValidator()
        {
            RuleFor(x => x.EquipeId)
                .GreaterThan(0).WithMessage("O identificador da equipe deve ser válido.");
        }
    }
}
