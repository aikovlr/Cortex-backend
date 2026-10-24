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

        public Task<bool> EnviarEmailAsync(string destinatario, string assunto, string mensagemCorpo)
        {
            _logger.LogInformation("[GATEWAY EMAIL STUB] Para: {Destinatario} | Assunto: '{Assunto}' | Conteúdo: {Corpo}",
                destinatario, assunto, mensagemCorpo);

            return Task.FromResult(true);
        }
    }
}
