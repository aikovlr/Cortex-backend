namespace Cortex.Entities
{
    public class Tarefa
    {
        public int Id {get; set;}
        public required string Titulo {get; set;}
        public string? Descricao {get; set;}
        public required DateTime DataVencimento {get; set;}
        public int? Pontuacao {get; set;}
        public Status Status {get; set;}
        public Prioridade Prioridade {get; set;}
        // navegacao
        public required int CriadorId { get; set; } // fk
        public required Usuario Criador { get; set; } // navegacao
        public ICollection<ResponsavelTarefa> ResponsaveisTarefas { get; set; } = new List<ResponsavelTarefa>();
        public ICollection<RespostaTarefa> Respostas { get; set; } = new List<RespostaTarefa>();
        public ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
        public ICollection<TicketReporte> Tickets { get; set; } = new List<TicketReporte>();
        public ICollection<Sugestao> Sugestoes { get; set; } = new List<Sugestao>();
        public ICollection<Meta> Metas { get; set; } = new List<Meta>();
    }
}