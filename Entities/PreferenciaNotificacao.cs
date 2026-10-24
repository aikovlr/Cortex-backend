namespace Cortex.Entities
{
    public class PreferenciaNotificacao
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public CanalNotificacao Canal { get; set; }
        public int AntecedenciaHoras { get; set; } = 24; // RN11: 1 a 168 horas
        public bool AtivoNovaAtribuicao { get; set; } = true;
        public bool AtivoVencimento { get; set; } = true;
        public bool AtivoConclusaoMeta { get; set; } = true;
        public bool AtivoTicket { get; set; } = true;

        // Navegação
        public Usuario Usuario { get; set; } = null!;
    }
}
