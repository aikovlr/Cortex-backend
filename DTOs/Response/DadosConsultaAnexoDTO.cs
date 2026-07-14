namespace Cortex.DTOs.Response
{
    public class ConsultaAnexoDTO
    {
        public int Id { get; set; }
        public required string UrlCaminho { get; set; }
        public required string NomeOriginal { get; set; }
        public required string MimeType { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}