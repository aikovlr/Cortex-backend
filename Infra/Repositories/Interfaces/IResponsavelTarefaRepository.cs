using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IResponsavelTarefaRepository
    {
        Task<ResponsavelTarefa?> ObterResponsavelTarefaPorIdAsync(int id);
        Task<List<ResponsavelTarefa>> ObterResponsaveisPorTarefaIdAsync(int tarefaId);
        Task<List<ResponsavelTarefa>> ObterTarefasPorUsuarioIdAsync(int usuarioId);
        Task<ResponsavelTarefa> AdicionarResponsavelTarefaAsync(ResponsavelTarefa responsavelTarefa);
        Task AdicionarResponsaveisEmLoteAsync(IEnumerable<ResponsavelTarefa> responsaveis);
        Task<bool> RemoverResponsavelTarefaAsync(int id);
        Task<bool> RemoverResponsavelPorTarefaEUsuarioAsync(int tarefaId, int usuarioId);
        Task<bool> ResponsavelTarefaExisteAsync(int id);
        Task<bool> ResponsavelJaAtribuidoAsync(int tarefaId, int usuarioId);
    }
}
