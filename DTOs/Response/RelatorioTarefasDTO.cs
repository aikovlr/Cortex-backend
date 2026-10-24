namespace Cortex.DTOs.Response
{
    public class RelatorioTarefasDTO
    {
        public int TotalTarefas { get; set; }
        public int Concluidas { get; set; }
        public int Pendentes { get; set; }
        public int Atrasadas { get; set; }
        public double TaxaConclusaoPercentual { get; set; }
        public Dictionary<string, int> PorPrioridade { get; set; } = new();
        public Dictionary<string, int> PorStatus { get; set; } = new();
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFim { get; set; }
        public DateTime GeradoEm { get; set; } = DateTime.UtcNow;
        public List<ConsultaTarefaDTO> Tarefas { get; set; } = [];
    }
}
