using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IMetaRepository
    {
        Task<Meta?> ObterMetaPorIdAsync(int id);
        
        Task<List<Meta>> ObterTodasMetasAsync();
        Task<List<Meta>> ObterMetasPorUsuarioIdAsync(int usuarioId);
        Task<Meta> AdicionarMetaAsync(Meta meta);
        Task<Meta> AtualizarMetaAsync(Meta meta);
        Task<bool> RemoverMetaAsync(int id);
        
        Task<bool> MetaExisteAsync(int id);
    }
}