using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class CriarMembroEquipeDTO
    {
        public int UsuarioId { get; set; }
        public int EquipeId { get; set; }
        public Cargo Cargo { get; set; } = Cargo.Membro;
    }
}