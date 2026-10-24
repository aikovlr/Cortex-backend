using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarUsuarioValidator : AbstractValidator<CriarUsuarioDTO>
    {
        public CriarUsuarioValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(255).WithMessage("O nome não pode exceder 255 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.")
                .MaximumLength(255).WithMessage("O e-mail não pode exceder 255 caracteres.");

            RuleFor(x => x.Senha)
                .ValidarComplexidadeSenha();

            RuleFor(x => x.Telefone)
                .MaximumLength(20).WithMessage("O telefone não pode exceder 20 caracteres.")
                .When(x => !string.IsNullOrEmpty(x.Telefone));
        }
    }
}
