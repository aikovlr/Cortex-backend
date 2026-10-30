using Cortex.DTOs.Request;
using FluentValidation;

namespace Cortex.DTOs.Validators
{
    public class PreferenciaNotificacaoValidator : AbstractValidator<ConfigurarPreferenciaDTO>
    {
        public PreferenciaNotificacaoValidator()
        {
            RuleFor(x => x.AntecedenciaHoras)
                .InclusiveBetween(1, 168)
                .WithMessage("A antecedência para disparo de notificações deve ser configurada entre 1 hora e 7 dias (1 a 168 horas).");

            RuleFor(x => x.Canal)
                .IsInEnum()
                .WithMessage("O canal de notificação selecionado é inválido.");

            RuleFor(x => x.Tema)
                .Must(t => t == null || t.Equals("light", StringComparison.OrdinalIgnoreCase) || t.Equals("dark", StringComparison.OrdinalIgnoreCase))
                .WithMessage("O tema selecionado deve ser 'light' ou 'dark'.");

            RuleFor(x => x.TamanhoFonte)
                .Must(f => !f.HasValue || (f.Value >= 12 && f.Value <= 24 && f.Value % 2 == 0))
                .WithMessage("O tamanho da fonte deve estar entre 12px e 24px e ser obrigatoriamente um número par (12, 14, 16, 18, 20, 22 ou 24).");
        }
    }
}
