namespace Cortex.DTOs.Response
{
    public class ConsultaUsuarioDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefone { get; set; }
        public bool EmailVerificado { get; set; }
        public int Nivel { get; set; }
        public int Experiencia { get; set; }
        public ConsultaAnexoDTO? FotoPerfil { get; set; }
    }
}