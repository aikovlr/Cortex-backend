namespace Cortex.Entities
{
    public class Equipe
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public ICollection<MembroEquipe> Membros { get; set; } = new List<MembroEquipe>();
    }
}