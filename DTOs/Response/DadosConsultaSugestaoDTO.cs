using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class ConsultaSugestaoDTO
    {
        public int Id { get; set; }
        public required string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public StatusSugestao Status { get; set; }
        public int TarefaId { get; set; }
        public int UsuarioId { get; set; }
        public ConsultaUsuarioDTO? Usuario { get; set; }
        public ConsultaTarefaDTO? Tarefa { get; set; }
    }
}