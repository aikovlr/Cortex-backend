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
                .ValidarEmailEstrito();

            RuleFor(x => x.Senha)
                .ValidarComplexidadeSenha();

            RuleFor(x => x.Telefone)
                .ValidarTelefoneOpcional()
                .When(x => !string.IsNullOrEmpty(x.Telefone));

            RuleFor(x => x.AceitouTermosPrivacidade)
                .Equal(true)
                .WithMessage("O aceite dos Termos de Uso e Política de Privacidade é obrigatório.");
        }
    }
}
