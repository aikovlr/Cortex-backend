using Cortex.DTOs;
using Cortex.Services.Interfaces;

namespace Cortex.Infra.Gateways
{
    public class EmailGatewayStub : IEmailGateway
    {
        private readonly ILogger<EmailGatewayStub> _logger;

        public EmailGatewayStub(ILogger<EmailGatewayStub> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
        {
            var emailMascarado = LogEmailGateway.MascararEmail(mensagem.Destinatario);
            _logger.LogInformation("[GATEWAY EMAIL STUB] Para: {Destinatario} | Assunto: '{Assunto}'",
                emailMascarado, mensagem.Assunto);

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
    }
}
