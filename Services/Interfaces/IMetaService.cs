using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface IMetaService
    {
        Task<ConsultaMetaDTO> CriarMetaAsync(CriarMetaDTO dto, int solicitanteId);
        Task<ConsultaMetaDTO?> ObterMetaPorIdAsync(int id, int solicitanteId);
        Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorUsuarioAsync(int usuarioId, int solicitanteId);
        Task<ConsultaMetaDTO> AtualizarMetaAsync(int id, AtualizarMetaDTO dto, int solicitanteId);
        Task<bool> DeletarMetaAsync(int id, int solicitanteId);
        Task<int> VerificarConclusaoMetaAsync(int tarefaId);
    }
}
