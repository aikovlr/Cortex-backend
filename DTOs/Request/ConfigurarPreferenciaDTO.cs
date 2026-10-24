using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class ConfigurarPreferenciaDTO
    {
        public CanalNotificacao Canal { get; set; }
        public int AntecedenciaHoras { get; set; } = 24;
        public bool AtivoNovaAtribuicao { get; set; } = true;
        public bool AtivoVencimento { get; set; } = true;
        public bool AtivoConclusaoMeta { get; set; } = true;
        public bool AtivoTicket { get; set; } = true;
    }
}
