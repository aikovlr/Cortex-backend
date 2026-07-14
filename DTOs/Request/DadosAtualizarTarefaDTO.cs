using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class AtualizarTarefaDTO
    {
        public required string Titulo {get; set;}
        public string? Descricao {get; set;}
        public required DateTime DataVencimento {get; set;}
        public int? Pontuacao {get; set;}
        public Prioridade Prioridade {get; set;}
        public List<IFormFile> Anexos {get; set;} = [];
    }
}