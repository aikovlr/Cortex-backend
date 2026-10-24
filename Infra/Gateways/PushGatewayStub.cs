using Cortex.Services.Interfaces;

namespace Cortex.Infra.Gateways
{
    public class PushGatewayStub : IPushGateway
    {
        private readonly ILogger<PushGatewayStub> _logger;

        public PushGatewayStub(ILogger<PushGatewayStub> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnviarPushAsync(int usuarioId, string titulo, string mensagem)
        {
            _logger.LogInformation("[GATEWAY PUSH STUB] UsuarioId: {UsuarioId} | Titulo: '{Titulo}' | Mensagem: {Mensagem}",
                usuarioId, titulo, mensagem);

            return Task.FromResult(true);
        }
    }
}
