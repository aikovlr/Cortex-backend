using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class CriarAnexoValidator : AbstractValidator<CriarAnexoDTO>
    {
        public CriarAnexoValidator()
        {
            RuleFor(x => x.Anexos)
                .NotEmpty().WithMessage("Ao menos um arquivo deve ser fornecido para upload.");

            RuleForEach(x => x.Anexos)
                .Must(arquivo => arquivo.Length > 0)
                .WithMessage("O arquivo de anexo não pode estar vazio.")
                .Must(arquivo => arquivo.Length <= 25 * 1024 * 1024)
                .WithMessage("O arquivo de anexo não pode exceder 25MB.");
        }
    }
}
