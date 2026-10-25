namespace Cortex.DTOs.Request
{
    public class SolicitarRedefinicaoSenhaDTO
    {
        public required string Email { get; set; }
    }

    public class RedefinirSenhaDTO
    {
        public required string Email { get; set; }
        public required string Codigo { get; set; }
        public required string NovaSenha { get; set; }
    }
}
