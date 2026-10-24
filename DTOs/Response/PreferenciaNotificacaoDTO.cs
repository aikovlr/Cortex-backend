using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class PreferenciaNotificacaoDTO
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public CanalNotificacao Canal { get; set; }
        public int AntecedenciaHoras { get; set; }
        public bool AtivoNovaAtribuicao { get; set; }
        public bool AtivoVencimento { get; set; }
        public bool AtivoConclusaoMeta { get; set; }
        public bool AtivoTicket { get; set; }
    }
}
