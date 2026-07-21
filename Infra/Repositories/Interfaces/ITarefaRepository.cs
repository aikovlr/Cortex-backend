using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface ITarefaRepository
    {
        Task<Tarefa?> ObterTarefaPorIdAsync(int id);
        
        Task<List<Tarefa>> ObterTodasTarefasAsync();
        Task<List<Tarefa>> ObterTarefasPorUsuarioIdAsync(int usuarioId);
        Task<Tarefa> AdicionarTarefaAsync(Tarefa tarefa);
        Task<Tarefa> AtualizarTarefaAsync(Tarefa tarefa);
        Task<bool> RemoverTarefaAsync(int id);
        
        Task<bool> TarefaExisteAsync(int id);
    }
}