using Cortex.DTOs.Request;
using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IRelatorioRepository
    {
        Task<List<Tarefa>> ObterTarefasParaRelatorioAsync(
            int? solicitanteId,
            bool ehAdminOuDono,
            FiltroRelatorioDTO filtro);

        Task<List<int>> ObterIdsMembrosDaEquipeAsync(int equipeId);

        Task<List<Tarefa>> ObterTarefasDaEquipeAsync(int equipeId, FiltroRelatorioDTO filtro);
    }
}
