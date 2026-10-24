namespace Cortex.Services.Interfaces
{
    public interface IEmailGateway
    {
        Task<bool> EnviarEmailAsync(string destinatario, string assunto, string mensagemCorpo);
    }
}
