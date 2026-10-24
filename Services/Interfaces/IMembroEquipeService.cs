using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;

namespace Cortex.Services.Interfaces
{
    public interface IMembroEquipeService
    {
        Task<ConsultaMembroEquipeDTO> AdicionarMembroAsync(CriarMembroEquipeDTO dto, int solicitanteId);
        Task<bool> RemoverMembroAsync(int membroId, int solicitanteId);
        Task<ConsultaMembroEquipeDTO> AlterarCargoAsync(int membroId, Cargo novoCargo, int solicitanteId);
        Task<IEnumerable<ConsultaMembroEquipeDTO>> ListarMembrosPorEquipeAsync(int equipeId, int? solicitanteId = null);
    }
}
