namespace Cortex.DTOs.Response
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = null!;
        public string Tipo { get; set; } = "Bearer";
        public ConsultaUsuarioDTO Usuario { get; set; } = null!;
    }
}
