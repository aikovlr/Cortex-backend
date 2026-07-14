namespace Cortex.DTOs.Response
{
    public class ConsultaRespostaTarefaDTO
    {
        public int Id{ get; set; }
        public DateTime DataResposta { get; set; }
        public int UsuarioId { get; set; }
        public int TarefaId { get; set; }
        public List<ConsultaAnexoDTO> Anexos {get; set;} = [];
    }
}