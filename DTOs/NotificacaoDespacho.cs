using Cortex.Entities;

namespace Cortex.DTOs
{
    public class NotificacaoDespacho
    {
        public CanalNotificacao Canal { get; set; }
        public int UsuarioId { get; set; }
        public int? TarefaId { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string? NomeDestinatario { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string? MensagemHtml { get; set; }
        public string MensagemTexto { get; set; } = string.Empty;
        public string TipoEvento { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}

