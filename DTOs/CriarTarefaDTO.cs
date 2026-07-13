using Cortex.Entities;

namespace Cortex.DTos
{
    public class CriarTarefaDTO
    {
        public string Titulo { get; set; } = null!;
        public string? Descricao { get; set; }
        public DateTime DataVencimento { get; set; }
        public int? Pontuacao { get; set; }
        public Status Status { get; set; }
        public Prioridade Prioridade { get; set; }
        public List<IFormFile>? Anexos { get; set; }
    }
}