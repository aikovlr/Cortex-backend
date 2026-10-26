namespace Cortex.Entities
{
    public class MetaTarefa
    {
        public int Id { get; set; }
        public int MetaId { get; set; }
        public Meta Meta { get; set; } = null!;
        public int TarefaId { get; set; }
        public Tarefa Tarefa { get; set; } = null!;
    }
}
