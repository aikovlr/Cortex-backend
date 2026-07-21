namespace Cortex.DTOs.Request
{
    public class CriarUsuarioDTO
    {
        public string Nome { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Senha { get; set; } = null!;
        public string? Telefone { get; set; }
        public IFormFile? FotoPerfil { get; set; }
    }
}

