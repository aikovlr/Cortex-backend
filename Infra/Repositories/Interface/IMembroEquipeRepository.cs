using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IMembroEquipeRepository
    {
        Task<MembroEquipe?> ObterMembroEquipePorIdAsync(int id);
        
        Task<List<MembroEquipe>> ObterMembrosEquipePorEquipeIdAsync(int equipeId);
        Task<MembroEquipe> AdicionarMembroEquipeAsync(MembroEquipe membroEquipe);
        Task<MembroEquipe> AtualizarMembroEquipeAsync(MembroEquipe membroEquipe);
        Task<bool> RemoverMembroEquipeAsync(int id);
        
        Task<bool> MembroEquipeExisteAsync(int id);
    }
}