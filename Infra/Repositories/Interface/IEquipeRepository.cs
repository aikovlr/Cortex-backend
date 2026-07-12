using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IEquipeRepository
    {
        Task<Equipe?> ObterEquipePorIdAsync(int id);
        
        Task<List<Equipe>> ObterTodasEquipesAsync();
        Task<List<Equipe>> ObterEquipesPorUsuarioIdAsync(int usuarioId);
        Task<Equipe> AdicionarEquipeAsync(Equipe equipe);
        Task<Equipe> AtualizarEquipeAsync(Equipe equipe);
        Task<bool> RemoverEquipeAsync(int id);
        
        Task<bool> EquipeExisteAsync(int id);
    }
}