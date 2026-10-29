using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface IRespostaTarefaService
    {
        Task<ConsultaRespostaTarefaDTO> CriarRespostaAsync(CriarRespostaTarefaDTO dto, int solicitanteId);
        Task<IEnumerable<ConsultaRespostaTarefaDTO>> ListarRespostasPorTarefaAsync(int tarefaId, int solicitanteId);
        Task<ConsultaRespostaTarefaDTO> AtribuirNotaAsync(int respostaId, int nota, int solicitanteId);
        Task<bool> DeletarRespostaAsync(int respostaId, int solicitanteId);
    }
}
