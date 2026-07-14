namespace Cortex.DTOs.Response
{
    public class ConsultaMetaDTO
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public DateTime DataFechamento { get; set; }
        public bool Concluida { get; set; }
        public int TarefaId { get; set; }
    }
}