using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class SolicitarRedefinicaoSenhaValidator : AbstractValidator<SolicitarRedefinicaoSenhaDTO>
    {
        public SolicitarRedefinicaoSenhaValidator()
        {
            RuleFor(x => x.Email)
                .ValidarEmailEstrito();
        }
    }

    public class RedefinirSenhaValidator : AbstractValidator<RedefinirSenhaDTO>
    {
        public RedefinirSenhaValidator()
        {
            RuleFor(x => x.Email)
                .ValidarEmailEstrito();

            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("O código de verificação é obrigatório.")
                .Length(6).WithMessage("O código deve possuir 6 dígitos.");

            RuleFor(x => x.NovaSenha)
                .ValidarComplexidadeSenha();
        }
    }
}
