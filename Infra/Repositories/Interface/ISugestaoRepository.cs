using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface ISugestaoRepository
    {
        Task<Sugestao?> ObterSugestaoPorIdAsync(int id);
       
        Task<List<Sugestao>> ObterSugestoesPorUsuarioIdAsync(int usuarioId);
        Task<List<Sugestao>> ObterSugestoesPorTarefaIdAsync(int tarefaId);
        Task<Sugestao> AdicionarSugestaoAsync(Sugestao sugestao);
        Task<Sugestao> AtualizarSugestaoAsync(Sugestao sugestao);
        Task<bool> RemoverSugestaoAsync(int id);
        
        Task<bool> SugestaoExisteAsync(int id);
    }
}