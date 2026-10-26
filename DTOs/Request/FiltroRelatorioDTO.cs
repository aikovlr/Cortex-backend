using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class FiltroRelatorioDTO
    {
        public Status? Status { get; set; }
        public Prioridade? Prioridade { get; set; }
        public int? EquipeId { get; set; }
        public int? ResponsavelId { get; set; }
        public int? CriadorId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }
}
