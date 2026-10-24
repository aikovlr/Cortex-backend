namespace Cortex.DTOs
{
    public class DadosLoginDTO
    {
        public string Email { get; set; } = null!;
        public string Senha { get; set; } = string.Empty;
        public string SenhaHash
        {
            get => Senha;
            set => Senha = value;
        }
    }
}