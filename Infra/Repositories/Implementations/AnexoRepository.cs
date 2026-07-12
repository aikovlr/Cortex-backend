using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IAnexoRepository
    {
        Task<Anexo?> ObterAnexoPorIdAsync(int id);
        
        Task<List<Anexo>> ObterAnexosPorTarefaIdAsync(int tarefaId);
        Task<List<Anexo>> ObterAnexosPorRespostaIdAsync(int respostaId);
        Task<List<Anexo>> ObterAnexosPorUsuarioIdAsync(int usuarioId);
        Task<Anexo> AdicionarAnexoAsync(Anexo anexo);
        Task<Anexo> AtualizarAnexoAsync(Anexo anexo);
        Task<bool> RemoverAnexoAsync(int id);
       
        Task<bool> AnexoExisteAsync(int id);
    }
}