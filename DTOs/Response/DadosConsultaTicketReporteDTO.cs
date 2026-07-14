namespace Cortex.DTOs.Response
{
    public class ConsultaTicketReporteDTO
    {
        public int Id {get; set;}
        public required string Descricao { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
        public ConsultaTarefaDTO Tarefa { get; set; } = null!;
        public ConsultaUsuarioDTO Usuario { get; set; } = null!;
    }
}