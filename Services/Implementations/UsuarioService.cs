using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordService _passwordService;

        public UsuarioService(IUsuarioRepository usuarioRepository, IPasswordService passwordService)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
        }

        public async Task<ConsultaUsuarioDTO> CriarUsuarioAsync(CriarUsuarioDTO dto)
        {
            // BR01: Email must be unique
            var usuarioExistente = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
            if (usuarioExistente != null)
                throw new InvalidOperationException("E-mail já cadastrado.");

            // BR02: Password validation
            var (isValid, errorMessage) = _passwordService.ValidatePassword(dto.Senha);
            if (!isValid)
                throw new ArgumentException(errorMessage);

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = _passwordService.HashPassword(dto.Senha),
                Telefone = dto.Telefone
            };

            if (dto.FotoPerfil != null)
            {
                usuario.FotoPerfil = new Anexo
                {
                    NomeOriginal = dto.FotoPerfil.FileName,
                    MimeType = dto.FotoPerfil.ContentType,
                    UrlCaminho = "uploads/usuarios/foto.png",
                    TipoEntidade = TipoEntidade.Usuario
                };
            }

            await _usuarioRepository.AdicionarUsuarioAsync(usuario);

            return new ConsultaUsuarioDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FotoPerfil = usuario.FotoPerfil
            };
        }

        public async Task<ConsultaUsuarioDTO?> ObterUsuarioPorIdAsync(int id)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
            if (usuario == null) return null;

            return new ConsultaUsuarioDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FotoPerfil = usuario.FotoPerfil
            };
        }

        public async Task<ConsultaUsuarioDTO?> ObterUsuarioPorEmailAsync(string email)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email);
            if (usuario == null) return null;

            return new ConsultaUsuarioDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FotoPerfil = usuario.FotoPerfil
            };
        }

        public async Task<IEnumerable<ConsultaUsuarioDTO>> ListarUsuariosAsync()
        {
            var usuarios = await _usuarioRepository.ListarUsuariosAsync();
            return usuarios.Select(u => new ConsultaUsuarioDTO
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                FotoPerfil = u.FotoPerfil
            });
        }

        public async Task<ConsultaUsuarioDTO> AtualizarUsuarioAsync(int id, AtualizarUsuarioDTO dto)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            // BR01: Email must be unique (if being changed)
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != usuario.Email)
            {
                var usuarioExistente = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
                if (usuarioExistente != null)
                    throw new InvalidOperationException("E-mail já cadastrado.");

                usuario.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.Nome))
                usuario.Nome = dto.Nome;

            if (!string.IsNullOrEmpty(dto.Telefone))
                usuario.Telefone = dto.Telefone;

            if (dto.FotoPerfil != null)
            {
                usuario.FotoPerfil = new Anexo
                {
                    NomeOriginal = dto.FotoPerfil.FileName,
                    MimeType = dto.FotoPerfil.ContentType,
                    UrlCaminho = "uploads/usuarios/foto.png",
                    TipoEntidade = TipoEntidade.Usuario
                };
            }

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            return new ConsultaUsuarioDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FotoPerfil = usuario.FotoPerfil
            };
        }

        public async Task<bool> DeletarUsuarioAsync(int id)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
                return false;

            // BR05: Block deletion if user has tasks "In Progress" as creator or assignee
            var temTarefasEmAndamento = await _usuarioRepository.UsuarioTemTarefasEmAndamentoAsync(id);
            var ehResponsavelPorTarefaEmAndamento = await _usuarioRepository.UsuarioEhResponsavelPorTarefaEmAndamentoAsync(id);

            if (temTarefasEmAndamento || ehResponsavelPorTarefaEmAndamento)
                throw new InvalidOperationException("Não é possível excluir usuário com tarefas em andamento.");

            var removido = await _usuarioRepository.RemoverUsuarioAsync(id);
            return removido;
        }

        public async Task<bool> ValidarSenhaAsync(string email, string senha)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email);
            if (usuario == null)
                return false;

            return _passwordService.VerifyPassword(senha, usuario.SenhaHash);
        }

        public async Task<bool> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            // Verify current password
            if (!_passwordService.VerifyPassword(senhaAtual, usuario.SenhaHash))
                throw new UnauthorizedAccessException("Senha atual incorreta.");

            // Validate new password
            var (isValid, errorMessage) = _passwordService.ValidatePassword(novaSenha);
            if (!isValid)
                throw new ArgumentException(errorMessage);

            // Hash and update new password
            usuario.SenhaHash = _passwordService.HashPassword(novaSenha);
            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            return true;
        }

        public async Task<bool> UsuarioTemTarefasEmAndamentoAsync(int usuarioId)
        {
            return await _usuarioRepository.UsuarioTemTarefasEmAndamentoAsync(usuarioId);
        }
    }
}