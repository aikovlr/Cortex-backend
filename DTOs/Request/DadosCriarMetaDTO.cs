namespace Cortex.DTOs.Request
{
    public class CriarMetaDTO
    {
        public string Nome {get; set;} = null!;
        public DateTime? DataFechamento  {get; set;}
        public int TarefaId {get; set;}
    }
}