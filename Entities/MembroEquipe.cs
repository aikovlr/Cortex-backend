namespace Cortex.Entities
{
    public class MembroEquipe
    {
        public int Id { get; set; }
        public DateTime DataEntrada { get; set; }
        public Cargo Cargo { get; set; }
        public int UsuarioId { get; set; }
        public int EquipeId { get; set; }
       // navegacao
        public Usuario Usuario { get; set; } = null!;
        public Equipe Equipe { get; set; } = null!;
    }
}