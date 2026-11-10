using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface IMetaService
    {
        Task<ConsultaMetaDTO> CriarMetaAsync(CriarMetaDTO dto, int solicitanteId);
        Task<ConsultaMetaDTO?> ObterMetaPorIdAsync(int id, int solicitanteId);
        Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorUsuarioAsync(int usuarioId, int solicitanteId);
        Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorTarefaAsync(int tarefaId, int solicitanteId);
        Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorEquipeAsync(int equipeId, int solicitanteId);
        Task<ConsultaMetaDTO> AtribuirEquipeAsync(int metaId, int equipeId, int solicitanteId, int? xpRecompensa = null);
        Task<ConsultaMetaDTO> AtualizarMetaAsync(int id, AtualizarMetaDTO dto, int solicitanteId);
        Task<bool> DeletarMetaAsync(int id, int solicitanteId);
        Task<int> VerificarConclusaoMetaAsync(int tarefaId);
    }
}
