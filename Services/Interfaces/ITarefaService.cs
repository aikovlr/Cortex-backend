using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface ITarefaService
    {
        Task<ConsultaTarefaDTO> CriarTarefaAsync(CriarTarefaDTO dto, int criadorId);
        Task<DetalhesTarefaDTO?> ObterTarefaPorIdAsync(int id, int? solicitanteId = null);
        Task<IEnumerable<ConsultaTarefaDTO>> ListarTarefasAsync(FiltroTarefasDTO? filtros = null, int? solicitanteId = null);
        Task<DetalhesTarefaDTO> AtualizarTarefaAsync(int id, AtualizarTarefaDTO dto, int solicitanteId);
        Task<bool> DeletarTarefaAsync(int id, int solicitanteId);
        Task<DetalhesTarefaDTO> ConcluirTarefaAsync(int id, int solicitanteId);
        Task<int> VerificarTarefasAtrasadasAsync();
    }
}
