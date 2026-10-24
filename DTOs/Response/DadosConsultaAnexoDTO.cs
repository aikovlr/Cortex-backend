using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class ConsultaAnexoDTO
    {
        public int Id { get; set; }
        public required string UrlCaminho { get; set; }
        public required string NomeOriginal { get; set; }
        public required string MimeType { get; set; }
        public DateTime DataEnvio { get; set; }
        public TipoEntidade TipoEntidade { get; set; }
        public int? TarefaId { get; set; }
        public int? RespostaTarefaId { get; set; }
        public int? UsuarioId { get; set; }
    }
}