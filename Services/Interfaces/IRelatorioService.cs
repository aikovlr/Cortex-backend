using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface IRelatorioService
    {
        Task<RelatorioTarefasDTO> GerarRelatorioTarefasAsync(FiltroRelatorioDTO filtro, int solicitanteId);
        Task<RelatorioEquipeDTO> GerarRelatorioEquipeAsync(int equipeId, FiltroRelatorioDTO filtro, int solicitanteId);
    }
}
