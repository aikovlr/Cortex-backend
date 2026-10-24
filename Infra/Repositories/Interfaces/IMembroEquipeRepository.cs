using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IMembroEquipeRepository
    {
        Task<MembroEquipe?> ObterMembroEquipePorIdAsync(int id);
        Task<MembroEquipe?> ObterMembroPorEquipeEUsuarioAsync(int equipeId, int usuarioId);
        Task<List<MembroEquipe>> ObterMembrosEquipePorEquipeIdAsync(int equipeId);
        Task<List<MembroEquipe>> ObterMembrosPorUsuarioIdAsync(int usuarioId);
        Task<MembroEquipe> AdicionarMembroEquipeAsync(MembroEquipe membroEquipe);
        Task<MembroEquipe> AtualizarMembroEquipeAsync(MembroEquipe membroEquipe);
        Task<bool> RemoverMembroEquipeAsync(int id);
        Task<bool> MembroEquipeExisteAsync(int id);
        Task<bool> MembroExisteNaEquipeAsync(int equipeId, int usuarioId);
    }
}