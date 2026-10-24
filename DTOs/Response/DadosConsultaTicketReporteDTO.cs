namespace Cortex.DTOs.Response
{
    public class ConsultaTicketReporteDTO
    {
        public int Id { get; set; }
        public required string Descricao { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
        public int TarefaId { get; set; }
        public int UsuarioId { get; set; }
        public ConsultaTarefaDTO? Tarefa { get; set; }
        public ConsultaUsuarioDTO? Usuario { get; set; }
    }
}