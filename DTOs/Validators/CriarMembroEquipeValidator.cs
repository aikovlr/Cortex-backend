using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarMembroEquipeValidator : AbstractValidator<CriarMembroEquipeDTO>
    {
        public CriarMembroEquipeValidator()
        {
            RuleFor(x => x.UsuarioId)
                .GreaterThan(0).WithMessage("O identificador do usuário deve ser válido.");

            RuleFor(x => x.EquipeId)
                .GreaterThan(0).WithMessage("O identificador da equipe deve ser válido.");

            RuleFor(x => x.Cargo)
                .IsInEnum().WithMessage("O cargo informado é inválido.");
        }
    }
}
