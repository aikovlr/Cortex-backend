using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface ISugestaoService
    {
        Task<ConsultaSugestaoDTO> CriarSugestaoAsync(CriarSugestaoDTO dto, int solicitanteId);
        Task<ConsultaSugestaoDTO> AprovarSugestaoAsync(int sugestaoId, int solicitanteId);
        Task<ConsultaSugestaoDTO> RejeitarSugestaoAsync(int sugestaoId, int solicitanteId);
        Task<IEnumerable<ConsultaSugestaoDTO>> ListarSugestoesPorTarefaAsync(int tarefaId, int solicitanteId);
    }
}
