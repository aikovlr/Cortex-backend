using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface IResponsavelTarefaService
    {
        Task<ConsultaResponsavelTarefaDTO> AtribuirResponsavelAsync(CriarResponsavelTarefaDTO dto, int solicitanteId);
        Task<IEnumerable<ConsultaResponsavelTarefaDTO>> AtribuirEquipeAsync(int tarefaId, int equipeId, int solicitanteId);
        Task<bool> RemoverResponsavelAsync(int responsavelId, int solicitanteId);
        Task<IEnumerable<ConsultaResponsavelTarefaDTO>> ListarResponsaveisPorTarefaAsync(int tarefaId);
    }
}
