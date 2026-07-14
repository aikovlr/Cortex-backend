using Cortex.Entities;

namespace Cortex.DTOs.Response
{
    public class ConsultaUsuarioDTO
    {
        public int Id {get; set;}
        public string Nome {get; set;} = null!;
        public string Email {get; set;} = null!;
        public Anexo? FotoPerfil {get; set;}
    }
}