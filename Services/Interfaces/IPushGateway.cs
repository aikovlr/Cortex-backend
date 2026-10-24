namespace Cortex.Services.Interfaces
{
    public interface IPushGateway
    {
        Task<bool> EnviarPushAsync(int usuarioId, string titulo, string mensagem);
    }
}
