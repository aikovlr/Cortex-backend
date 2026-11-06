using Cortex.DTOs;
using Cortex.Services.Interfaces;

namespace Cortex.Infra.Gateways
{
    public class LogEmailGateway : IEmailGateway
    {
        private readonly ILogger<LogEmailGateway> _logger;

        public LogEmailGateway(ILogger<LogEmailGateway> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
        {
            var emailMascarado = MascararEmail(mensagem.Destinatario);
            _logger.LogInformation(
                "[EMAIL LOG GATEWAY] Destinatário: {Destinatario} | Assunto: '{Assunto}' | HTML Size: {HtmlSize} chars | Texto: {HasPlainText}",
                emailMascarado,
                mensagem.Assunto,
                mensagem.CorpoHtml?.Length ?? 0,
                !string.IsNullOrWhiteSpace(mensagem.CorpoTexto));

            return Task.FromResult(true);
        }

        public Task<bool> EnviarEmailAsync(string destinatario, string assunto, string mensagemCorpo)
        {
            return EnviarAsync(new EmailMensagem
            {
                Destinatario = destinatario,
                Assunto = assunto,
                CorpoHtml = $"<p>{mensagemCorpo}</p>",
                CorpoTexto = mensagemCorpo
            });
        }

        public static string MascararEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "***";
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***" + (atIndex >= 0 ? email[atIndex..] : "");
            return $"{email[0]}***{email[atIndex..]}";
        }
    }
}

