using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IRespostaTarefaRepository
    {
        Task<RespostaTarefa?> ObterRespostaTarefaPorIdAsync(int id);
        
        Task<List<RespostaTarefa>> ObterRespostasTarefaPorTarefaIdAsync(int tarefaId);
        Task <List<RespostaTarefa>> ObterRespostasTarefaPorUsuarioIdAsync(int usuarioId);
        Task<RespostaTarefa> AdicionarRespostaTarefaAsync(RespostaTarefa respostaTarefa);
        Task<RespostaTarefa> AtualizarRespostaTarefaAsync(RespostaTarefa respostaTarefa);
        Task<bool> RemoverRespostaTarefaAsync(int id);
        
        Task<bool> RespostaTarefaExisteAsync(int id);
    }
}