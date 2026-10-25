namespace Cortex.Services.Interfaces
{
    public interface IVerificacaoEmailService
    {
        /// <summary>Gera um código de 6 dígitos, armazena o hash no usuário e retorna o código em texto claro para envio.</summary>
        Task<string> GerarEArmazenarCodigoAsync(int usuarioId);

        /// <summary>Verifica o código recebido contra o hash armazenado e marca o e-mail como verificado.</summary>
        Task<bool> VerificarCodigoAsync(string email, string codigoRecebido);
    }
}
