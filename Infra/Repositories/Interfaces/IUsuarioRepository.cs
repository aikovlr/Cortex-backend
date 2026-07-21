using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObterUsuarioPorIdAsync(int id);
        Task<Usuario?> ObterUsuarioPorEmailAsync(string email);
        
        Task<List<Usuario>> ObterTodosUsuariosAsync();
        Task<List<Usuario>> ListarUsuariosAsync();
        Task<Usuario> AdicionarUsuarioAsync(Usuario usuario);
        Task<Usuario> AtualizarUsuarioAsync(Usuario usuario);
        Task<bool> RemoverUsuarioAsync(int id);
        
        Task<bool> UsuarioExisteAsync(int id);
        Task<bool> UsuarioExistePorEmailAsync(string email);
        Task<bool> UsuarioTemTarefasEmAndamentoAsync(int usuarioId);
        Task<bool> UsuarioEhResponsavelPorTarefaEmAndamentoAsync(int usuarioId);
    }
}