namespace Cortex.DTOs.Response
{
    public class ConsultaMetaDTO
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public DateTime? DataFechamento { get; set; }
        public bool Concluida { get; set; }
        public int TarefaId { get; set; }
        public int UsuarioId { get; set; }
        public ConsultaTarefaDTO? Tarefa { get; set; }
        public List<ConsultaTarefaDTO> Tarefas { get; set; } = new();
        public ConsultaUsuarioDTO? Usuario { get; set; }
    }
}