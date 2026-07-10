namespace Cortex.Entities
{
    public class Meta
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public DateTime? DataFechamento { get; set; }
        public bool? Concluida { get; set; }
        public int TarefaId { get; set; }
        public int UsuarioId { get; set; }
        // navegacao
        public Usuario Usuario { get; set; } = null!;
        public Tarefa Tarefa { get; set; } = null!;
    }
}