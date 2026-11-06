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
        public bool AtivoSugestao { get; set; } = true;
        public bool AtivoEquipe { get; set; } = true;
        public bool AtivoGeral { get; set; } = true;
        public string Tema { get; set; } = "light";
        public bool AltoContraste { get; set; } = false;
        public int TamanhoFonte { get; set; } = 16;
    }
}
