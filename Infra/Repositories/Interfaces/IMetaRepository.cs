using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IMetaRepository
    {
        Task<Meta?> ObterMetaPorIdAsync(int id);
        Task<List<Meta>> ObterTodasMetasAsync();
        Task<List<Meta>> ObterMetasPorUsuarioIdAsync(int usuarioId);
        Task<List<Meta>> ObterMetasPorTarefaIdAsync(int tarefaId);
        Task<List<Meta>> ObterMetasPorEquipeIdAsync(int equipeId);
        Task<List<Tarefa>> ObterTarefasDaMetaAsync(int metaId);
        Task<Meta> AdicionarMetaAsync(Meta meta);
        Task<Meta> AtualizarMetaAsync(Meta meta);
        Task<bool> RemoverMetaAsync(int id);
        Task<bool> MetaExisteAsync(int id);
        Task AdicionarMetaEquipeAsync(MetaEquipe metaEquipe);
        Task<bool> MetaEquipeExisteAsync(int metaId, int equipeId);
        Task<List<Equipe>> ObterEquipesDaMetaAsync(int metaId);
    }
}
