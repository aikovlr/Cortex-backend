using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarUsuarioValidator : AbstractValidator<AtualizarUsuarioDTO>
    {
        public AtualizarUsuarioValidator()
        {
            RuleFor(x => x.Nome)
                .MaximumLength(255).WithMessage("O nome não pode exceder 255 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Nome));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Formato de e-mail inválido.")
                .MaximumLength(255).WithMessage("O e-mail não pode exceder 255 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Senha!)
                .ValidarComplexidadeSenha()
                .When(x => !string.IsNullOrEmpty(x.Senha));

            RuleFor(x => x.Telefone)
                .MaximumLength(20).WithMessage("O telefone não pode exceder 20 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Telefone));
        }
    }
}
