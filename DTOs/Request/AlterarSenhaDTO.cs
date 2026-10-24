namespace Cortex.DTOs.Request
{
    public class AlterarSenhaDTO
    {
        public string SenhaAtual { get; set; } = null!;
        public string NovaSenha { get; set; } = null!;
    }
}
