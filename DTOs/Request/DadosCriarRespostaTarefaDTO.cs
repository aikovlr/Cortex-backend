namespace Cortex.DTOs.Request
{
    public class CriarRespostaTarefaDTO
    {
        public int TarefaId { get; set; }
        public List<IFormFile>? Anexos { get; set; }
    }
}