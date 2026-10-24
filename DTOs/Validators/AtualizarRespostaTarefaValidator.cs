using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class AtualizarRespostaTarefaValidator : AbstractValidator<AtualizarRespostaTarefaDTO>
    {
        public AtualizarRespostaTarefaValidator()
        {
            RuleForEach(x => x.Anexos)
                .Must(arquivo => arquivo.Length > 0)
                .WithMessage("O arquivo de anexo não pode estar vazio.")
                .Must(arquivo => arquivo.Length <= 25 * 1024 * 1024)
                .WithMessage("O arquivo de anexo não pode exceder 25MB.");
        }
    }
}
