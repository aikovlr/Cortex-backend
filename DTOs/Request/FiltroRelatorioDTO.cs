using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class FiltroRelatorioDTO
    {
        public Status? Status { get; set; }
        public Prioridade? Prioridade { get; set; }
        public int? EquipeId { get; set; }
        public int? ResponsavelId { get; set; }
        public List<int>? ResponsaveisIds { get; set; }
        public List<int>? ResponsavelIds { get; set; }
        public int? CriadorId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public List<int> ObterIdsResponsaveis()
        {
            var ids = new List<int>();
            if (ResponsavelId.HasValue && ResponsavelId.Value > 0)
                ids.Add(ResponsavelId.Value);

            if (ResponsaveisIds != null && ResponsaveisIds.Count > 0)
                ids.AddRange(ResponsaveisIds.Where(id => id > 0));

            if (ResponsavelIds != null && ResponsavelIds.Count > 0)
                ids.AddRange(ResponsavelIds.Where(id => id > 0));

            return ids.Distinct().ToList();
        }
    }
}
