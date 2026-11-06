using Cortex.DTOs;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class DadosLoginValidator : AbstractValidator<DadosLoginDTO>
    {
        public DadosLoginValidator()
        {
            RuleFor(x => x.Email)
                .ValidarEmailEstrito();

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("A senha é obrigatória.");
        }
    }
}
