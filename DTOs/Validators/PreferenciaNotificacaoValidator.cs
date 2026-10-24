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
                .WithMessage("A antecedência para disparo de notificações deve ser configurada entre 1 hora e 7 dias (1 a 168 horas) (RN11).");

            RuleFor(x => x.Canal)
                .IsInEnum()
                .WithMessage("O canal de notificação selecionado é inválido.");
        }
    }
}
