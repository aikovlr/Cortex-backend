namespace Cortex.Services.Interfaces
{
    public interface ISmsGateway
    {
        Task<bool> EnviarSmsAsync(string telefone, string mensagem);
    }
}
