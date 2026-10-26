using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarMembroEquipeValidator : AbstractValidator<CriarMembroEquipeDTO>
    {
        public CriarMembroEquipeValidator()
        {
            RuleFor(x => x)
                .Must(x => (x.UsuarioId.HasValue && x.UsuarioId.Value > 0) || !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("Informe o identificador do usuário ou o e-mail.");

            RuleFor(x => x.Cargo)
                .IsInEnum().WithMessage("O cargo informado é inválido.");
        }
    }
}
