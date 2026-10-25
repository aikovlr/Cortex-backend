using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class SolicitarRedefinicaoSenhaValidator : AbstractValidator<SolicitarRedefinicaoSenhaDTO>
    {
        public SolicitarRedefinicaoSenhaValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.");
        }
    }

    public class RedefinirSenhaValidator : AbstractValidator<RedefinirSenhaDTO>
    {
        public RedefinirSenhaValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.");

            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("O código de verificação é obrigatório.")
                .Length(6).WithMessage("O código deve possuir 6 dígitos.");

            RuleFor(x => x.NovaSenha)
                .ValidarComplexidadeSenha();
        }
    }
}
