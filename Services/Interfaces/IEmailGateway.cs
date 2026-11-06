using Cortex.DTOs;

namespace Cortex.Services.Interfaces
{
    public interface IEmailGateway
    {
        Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default);

        Task<bool> EnviarEmailAsync(string destinatario, string assunto, string mensagemCorpo);
    }
}
