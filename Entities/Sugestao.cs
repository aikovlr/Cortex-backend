namespace Cortex.Entities
{
    public class Sugestao
    {
        public int Id{ get; set; }
        public required string Descricao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public int TarefaId { get; set; }
        public int UsuarioId { get; set; }
        public Tarefa Tarefa { get; set; } = null!;
        public Usuario Usuario { get; set; } = null!;
    }
}