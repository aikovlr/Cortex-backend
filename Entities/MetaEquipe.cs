namespace Cortex.Entities
{
    public class MetaEquipe
    {
        public int Id { get; set; }
        public int MetaId { get; set; }
        public Meta Meta { get; set; } = null!;
        public int EquipeId { get; set; }
        public Equipe Equipe { get; set; } = null!;
        public int XpRecompensa { get; set; } = 0;
        public DateTime DataVinculo { get; set; } = DateTime.UtcNow;
    }
}

