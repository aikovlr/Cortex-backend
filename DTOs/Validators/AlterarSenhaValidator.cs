using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AlterarSenhaValidator : AbstractValidator<AlterarSenhaDTO>
    {
        public AlterarSenhaValidator()
        {
            RuleFor(x => x.SenhaAtual)
                .NotEmpty().WithMessage("A senha atual é obrigatória.");

            RuleFor(x => x.NovaSenha)
                .ValidarComplexidadeSenha();
        }
    }
}
