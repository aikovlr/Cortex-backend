using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class PreferenciaAcessibilidadeValidator : AbstractValidator<AtualizarPreferenciaAcessibilidadeDTO>
    {
        public PreferenciaAcessibilidadeValidator()
        {
            RuleFor(x => x.Tema)
                .Must(t => t == null || t.Equals("light", StringComparison.OrdinalIgnoreCase) || t.Equals("dark", StringComparison.OrdinalIgnoreCase))
                .WithMessage("O tema selecionado deve ser 'light' ou 'dark'.");

            RuleFor(x => x.TamanhoFonte)
                .Must(f => !f.HasValue || (f.Value >= 12 && f.Value <= 24 && f.Value % 2 == 0))
                .WithMessage("O tamanho da fonte deve estar entre 12px e 24px e ser obrigatoriamente um número par (12, 14, 16, 18, 20, 22 ou 24).");
        }
    }
}

