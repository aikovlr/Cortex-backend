namespace Cortex.DTOs.Request
{
    public class CriarSugestaoDTO
    {
        public string Descricao { get; set; } = null!;
        public int TarefaId { get; set; }
    }
}