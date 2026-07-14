namespace Cortex.DTOs.Response
{
    public class ConsultaEquipeDTO
    {
        public int Id { get; set; }
        public required string Nome { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<ConsultaMembroEquipeDTO> Membros { get; set; } = [];
    }
}