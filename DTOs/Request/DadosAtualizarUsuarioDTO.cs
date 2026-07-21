namespace Cortex.DTOs.Request
{
    public class AtualizarUsuarioDTO
    {
        public string? Nome {get; set;}
        public string? Email {get; set;}
        public string? Senha {get; set;}
        public string? Telefone {get; set;}
        public IFormFile? FotoPerfil {get; set;}
    }
}