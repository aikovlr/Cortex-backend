namespace Cortex.Entities
{
    public class RespostaTarefa
    {
        public int Id{ get; set; }
        public DateTime DataResposta { get; set; } = DateTime.UtcNow;
        public int UsuarioId { get; set; }
        public int TarefaId { get; set; }
        // navegacao
        public Usuario Usuario { get; set; } = null!;
        public Tarefa Tarefa { get; set; } = null!;
        public ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
    }
}