using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class AtualizarTarefaDTO
    {
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public DateTime? DataVencimento { get; set; }   // nullable — não sobrescreve se null
        public int? Pontuacao { get; set; }
        public Prioridade? Prioridade { get; set; }     // nullable
        public List<IFormFile>? Anexos { get; set; }
    }
}