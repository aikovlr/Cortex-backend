using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;

namespace Cortex.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<ConsultaUsuarioDTO> CriarUsuarioAsync(CriarUsuarioDTO dto);
        Task<ConsultaUsuarioDTO?> ObterUsuarioPorIdAsync(int id);
        Task<ConsultaUsuarioDTO?> ObterUsuarioPorEmailAsync(string email);
        Task<IEnumerable<ConsultaUsuarioDTO>> ListarUsuariosAsync();
        Task<ConsultaUsuarioDTO> AtualizarUsuarioAsync(int id, AtualizarUsuarioDTO dto);
        Task<bool> DeletarUsuarioAsync(int id);
        Task<bool> ValidarSenhaAsync(string email, string senha);
        Task<bool> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha);
        Task<bool> UsuarioTemTarefasEmAndamentoAsync(int usuarioId);
    }
}