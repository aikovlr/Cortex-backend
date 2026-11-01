namespace Cortex.DTOs.Request
{
    public class AtualizarMetaDTO
    {
        public string? Nome { get; set; }
        public DateTime? DataFechamento { get; set; }
        public bool? Concluida { get; set; }
        public int TarefaId { get; set; }
        public List<int>? TarefaIds { get; set; }
        public int UsuarioId { get; set; }
        public bool LimparDataFechamento { get; set; } = false;
    }
}
