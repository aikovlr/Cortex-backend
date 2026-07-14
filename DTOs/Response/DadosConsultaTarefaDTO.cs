using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class ConsultaTarefaDTO
    {
        public int Id {get; set;}
        public required string Titulo {get; set;}
        public string? Descricao {get; set;}
        public required DateTime DataVencimento {get; set;}
        public int? Pontuacao {get; set;}
        public Status Status {get; set;}
        public Prioridade Prioridade {get; set;}
        public int CriadorId {get; set;}
    }
}