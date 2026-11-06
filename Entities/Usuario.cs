namespace Cortex.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public string? Telefone { get; set; }
        public required string Email { get; set; }
        public required string SenhaHash { get; set; }
        public bool EmailVerificado { get; set; } = false;
        public string? CodigoVerificacaoHash { get; set; }
        public DateTime? CodigoVerificacaoExpiracao { get; set; }

        // Redefinição de Senha (RF001 - Campos segregados de verificação)
        public string? CodigoRedefinicaoHash { get; set; }
        public DateTime? CodigoRedefinicaoExpiracao { get; set; }
        public int TentativasRedefinicao { get; set; } = 0;
        public DateTime? UltimaSolicitacaoRedefinicaoEm { get; set; }
        public DateTime? SenhaAlteradaEm { get; set; }

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
        public ICollection<PreferenciaNotificacao> PreferenciasNotificacao { get; set; } = new List<PreferenciaNotificacao>();
        public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();
    }
}
