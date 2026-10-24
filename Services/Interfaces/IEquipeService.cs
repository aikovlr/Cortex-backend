using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface IEquipeService
    {
        Task<ConsultaEquipeDTO> CriarEquipeAsync(CriarEquipeDTO dto, int criadorId);
        Task<ConsultaEquipeDTO?> ObterEquipePorIdAsync(int id, int? solicitanteId = null);
        Task<IEnumerable<ConsultaEquipeDTO>> ListarEquipesAsync(int? solicitanteId = null);
        Task<ConsultaEquipeDTO> AtualizarEquipeAsync(int id, AtualizarEquipeDTO dto, int solicitanteId);
        Task<bool> DeletarEquipeAsync(int id, int solicitanteId);
    }
}
