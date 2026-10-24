using Cortex.Services.Interfaces;

namespace Cortex.Infra.Gateways
{
    public class SmsGatewayStub : ISmsGateway
    {
        private readonly ILogger<SmsGatewayStub> _logger;

        public SmsGatewayStub(ILogger<SmsGatewayStub> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnviarSmsAsync(string telefone, string mensagem)
        {
            _logger.LogInformation("[GATEWAY SMS STUB] Telefone: {Telefone} | Mensagem: {Mensagem}",
                telefone, mensagem);

            return Task.FromResult(true);
        }
    }
}
