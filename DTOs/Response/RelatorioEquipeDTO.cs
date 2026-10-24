using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class RelatorioEquipeDTO
    {
        public int EquipeId { get; set; }
        public required string EquipeNome { get; set; }
        public int TotalMembros { get; set; }
        public RelatorioTarefasDTO MetricasGerais { get; set; } = new();
        public List<DesempenhoMembroDTO> DesempenhoMembros { get; set; } = [];
    }

    public class DesempenhoMembroDTO
    {
        public int UsuarioId { get; set; }
        public required string Nome { get; set; }
        public required string Email { get; set; }
        public Cargo Cargo { get; set; }
        public int TotalAtribuidas { get; set; }
        public int Concluidas { get; set; }
        public int Pendentes { get; set; }
        public int Atrasadas { get; set; }
        public double TaxaConclusaoPercentual { get; set; }
    }
}
