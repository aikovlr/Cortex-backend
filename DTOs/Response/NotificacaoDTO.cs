namespace Cortex.DTOs.Response
{
    public class NotificacaoDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public bool Lida { get; set; }
        public bool Destaque { get; set; }
        public int? TarefaId { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}

