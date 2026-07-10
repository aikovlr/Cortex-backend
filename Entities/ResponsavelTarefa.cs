namespace Cortex.Entities
{
    public class ResponsavelTarefa
    {
        public int Id{ get; set; }
        public int UsuarioId { get; set; }
        public int TarefaId { get; set; }
        // navegacao
        public Usuario Usuario { get; set; } = null!;
        public Tarefa Tarefa { get; set; } = null!;
    }
}