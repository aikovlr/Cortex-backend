namespace Cortex.DTOs.Request
{
    public class AtualizarMetaDTO
    {
        public required string Nome {get; set;}
        public DateTime? DataFechamento {get; set;}
        public bool? Concluida {get; set;}
        public int TarefaId {get; set;}
        public int UsuarioId {get; set;}
    }
}
