namespace Cortex.Entities
{
    public class TicketReporte
    {
        public int Id{ get; set; }
        public required string Descricao { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public required int TarefaId { get; set; } // fk
        public required int UsuarioId { get; set; } // fk
        // navegacao
        public required Tarefa Tarefa { get; set; } = null!;
        public required Usuario Usuario { get; set; }
    }
}