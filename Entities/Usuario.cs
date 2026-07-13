namespace Cortex.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public string? Telefone { get; set; }
        public required string Email { get; set; }
        public required string SenhaHash { get; set; }
        public Anexo? FotoPerfil { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        // navegacao
        public ICollection<Tarefa> TarefasCriadas { get; set; } = new List<Tarefa>();
        public ICollection<RespostaTarefa> Respostas { get; set; } = new List<RespostaTarefa>();
        public ICollection<ResponsavelTarefa> ResponsaveisTarefas { get; set; } = new List<ResponsavelTarefa>();
        public ICollection<MembroEquipe> Equipes { get; set; } = new List<MembroEquipe>();
        public ICollection<TicketReporte> Tickets { get; set; } = new List<TicketReporte>();
        public ICollection<Sugestao> Sugestoes { get; set; } = new List<Sugestao>();
        public ICollection<Meta> Metas { get; set; } = new List<Meta>();
    }
}