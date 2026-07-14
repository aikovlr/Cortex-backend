using Cortex.Entities;

namespace Cortex.DTOs.Request
{
    public class CriarMembroEquipeDTO
    {
        public Cargo Cargo {get; set;}
        public int EquipeId {get; set;}
    }
}