namespace Cortex.Entities
{
    public class AlertaEnviado
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int TarefaId { get; set; }
        public CanalNotificacao Canal { get; set; }
        public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public Usuario Usuario { get; set; } = null!;
        public Tarefa Tarefa { get; set; } = null!;
    }
}
