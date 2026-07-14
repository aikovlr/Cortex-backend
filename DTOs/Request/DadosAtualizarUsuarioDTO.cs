namespace Cortex.DTOs.Request
{
    public class AtualizarUsuarioDTO
    {
        public string? Nome {get; set;}
        public string? SenhaHash {get; set;}
        public string? Telefone {get; set;}
        public IFormFile? FotoPerfil {get; set;}
    }
}