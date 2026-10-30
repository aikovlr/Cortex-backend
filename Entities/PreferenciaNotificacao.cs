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

        // Preferências de Acessibilidade (Sincronização entre dispositivos)
        public string Tema { get; set; } = "light";
        public bool AltoContraste { get; set; } = false;
        public int TamanhoFonte { get; set; } = 16; // 12px a 24px (números pares)

        // Navegação
        public Usuario Usuario { get; set; } = null!;
    }
}
