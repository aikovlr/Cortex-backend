namespace Cortex.Entities
{
    public class Notificacao
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public string Tipo { get; set; } = string.Empty; // "tarefa", "tarefa_vencida", "tarefa_urgente", "resposta_ticket", "ticket_criador", "sugestao"
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public bool Lida { get; set; } = false;
        public bool Destaque { get; set; } = false;
        public int? TarefaId { get; set; }
        public Tarefa? Tarefa { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}

