using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class ConsultaMembroEquipeDTO
    {
        public int Id { get; set; }
        public DateTime DataEntrada { get; set; }
        public Cargo Cargo { get; set; }
        public int UsuarioId { get; set; }
        public int EquipeId { get; set; }
    }
}