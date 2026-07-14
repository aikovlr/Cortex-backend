namespace Cortex.DTOs.Response
{
    public class ConsultaSugestaoDTO
    {
        public int Id{ get; set; }
        public required string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public int TarefaId { get; set; }
        public int UsuarioId { get; set; }
    }
}