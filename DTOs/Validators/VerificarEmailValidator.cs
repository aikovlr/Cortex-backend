using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class VerificarEmailValidator : AbstractValidator<VerificarEmailDTO>
    {
        public VerificarEmailValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("O e-mail informado é inválido.");

            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("O código de verificação é obrigatório.")
                .Length(6).WithMessage("O código de verificação deve conter 6 caracteres.");
        }
    }

    public class SolicitarCodigoVerificacaoValidator : AbstractValidator<SolicitarCodigoVerificacaoDTO>
    {
        public SolicitarCodigoVerificacaoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress().WithMessage("O e-mail informado é inválido.");
        }
    }
}
