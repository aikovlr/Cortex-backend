using Cortex.DTOs;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class DadosLoginValidator : AbstractValidator<DadosLoginDTO>
    {
        public DadosLoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("Formato de e-mail inválido.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("A senha é obrigatória.");
        }
    }
}
