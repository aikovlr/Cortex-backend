using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarSugestaoValidator : AbstractValidator<AtualizarSugestaoDTO>
    {
        public AtualizarSugestaoValidator()
        {
            When(x => !string.IsNullOrEmpty(x.Descricao), () =>
            {
                RuleFor(x => x.Descricao!)
                    .MaximumLength(1000).WithMessage("A descrição da sugestão não pode exceder 1000 caracteres.");
            });
        }
    }
}
