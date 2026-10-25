namespace Cortex.DTOs.Request
{
    public class VerificarEmailDTO
    {
        public required string Email { get; set; }
        public required string Codigo { get; set; }
    }

    public class SolicitarCodigoVerificacaoDTO
    {
        public required string Email { get; set; }
    }

    public class ConfirmarCodigoDTO
    {
        public required string Codigo { get; set; }
    }
}
