using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Cortex.Services.Implementations
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordService _passwordService;
        private readonly IArquivoValidacaoService _arquivoValidacaoService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(
            IUsuarioRepository usuarioRepository, 
            IPasswordService passwordService,
            IArquivoValidacaoService arquivoValidacaoService,
            IWebHostEnvironment environment,
            ILogger<UsuarioService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
            _arquivoValidacaoService = arquivoValidacaoService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ConsultaUsuarioDTO> CriarUsuarioAsync(CriarUsuarioDTO dto)
        {
            // RN01: Email must be unique
            var usuarioExistente = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
            if (usuarioExistente != null)
                throw new InvalidOperationException("E-mail já cadastrado.");

            // RN02: Password validation
            var (isValid, errorMessage) = _passwordService.ValidatePassword(dto.Senha);
            if (!isValid)
                throw new ArgumentException(errorMessage);

            var usuario = new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = _passwordService.HashPassword(dto.Senha),
                Telefone = dto.Telefone,
                DataCriacao = DateTime.UtcNow
            };

            if (dto.FotoPerfil != null)
            {
                usuario.FotoPerfil = await SalvarFotoPerfilAsync(dto.FotoPerfil);
            }

            await _usuarioRepository.AdicionarUsuarioAsync(usuario);

            return MapToConsultaDTO(usuario);
        }

        public async Task<ConsultaUsuarioDTO?> ObterUsuarioPorIdAsync(int id)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
            return usuario == null ? null : MapToConsultaDTO(usuario);
        }

        public async Task<ConsultaUsuarioDTO?> ObterUsuarioPorEmailAsync(string email)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email);
            return usuario == null ? null : MapToConsultaDTO(usuario);
        }

        public async Task<IEnumerable<ConsultaUsuarioDTO>> ListarUsuariosAsync()
        {
            var usuarios = await _usuarioRepository.ListarUsuariosAsync();
            return usuarios.Select(MapToConsultaDTO);
        }

        public async Task<ConsultaUsuarioDTO> AtualizarUsuarioAsync(int id, AtualizarUsuarioDTO dto)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            // RN01: Email must be unique (if being changed)
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

            if (!string.IsNullOrEmpty(dto.Senha))
            {
                var (isValid, errorMessage) = _passwordService.ValidatePassword(dto.Senha);
                if (!isValid)
                    throw new ArgumentException(errorMessage);

                usuario.SenhaHash = _passwordService.HashPassword(dto.Senha);
            }

            if (dto.FotoPerfil != null)
            {
                usuario.FotoPerfil = await SalvarFotoPerfilAsync(dto.FotoPerfil);
            }

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            return MapToConsultaDTO(usuario);
        }

        public async Task<bool> DeletarUsuarioAsync(int id, int solicitanteId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
            if (usuario == null)
                return false;

            // RN05: Block deletion if user has tasks "In Progress" or "Pendente" as creator or assignee
            var temTarefasEmAndamento = await _usuarioRepository.UsuarioTemTarefasEmAndamentoAsync(id);
            var ehResponsavelPorTarefaEmAndamento = await _usuarioRepository.UsuarioEhResponsavelPorTarefaEmAndamentoAsync(id);

            if (temTarefasEmAndamento || ehResponsavelPorTarefaEmAndamento)
                throw new InvalidOperationException("Não é possível excluir usuário com tarefas em andamento.");

            var removido = await _usuarioRepository.RemoverUsuarioAsync(id);
            if (removido)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "Usuario", id, solicitanteId);
            }

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

            // Verify new password is not equal to current password
            if (_passwordService.VerifyPassword(novaSenha, usuario.SenhaHash))
                throw new ArgumentException("A nova senha não pode ser igual à senha atual.");

            // Validate new password (RN02)
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


        private async Task<Anexo> SalvarFotoPerfilAsync(IFormFile arquivo)
        {
            _arquivoValidacaoService.ValidarImagemPerfil(arquivo);

            var nomeOriginalSanitizado = Path.GetFileName(arquivo.FileName);
            var extensao = Path.GetExtension(nomeOriginalSanitizado).ToLowerInvariant();

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "usuarios");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(uploadDir, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return new Anexo
            {
                NomeOriginal = arquivo.FileName,
                MimeType = arquivo.ContentType,
                UrlCaminho = $"/uploads/usuarios/{nomeArquivo}",
                TipoEntidade = TipoEntidade.Usuario,
                DataEnvio = DateTime.UtcNow
            };
        }

        private static ConsultaUsuarioDTO MapToConsultaDTO(Usuario usuario)
        {
            return new ConsultaUsuarioDTO
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                EmailVerificado = usuario.EmailVerificado,
                FotoPerfil = usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                {
                    Id = usuario.FotoPerfil.Id,
                    UrlCaminho = usuario.FotoPerfil.UrlCaminho,
                    NomeOriginal = usuario.FotoPerfil.NomeOriginal,
                    MimeType = usuario.FotoPerfil.MimeType,
                    DataEnvio = usuario.FotoPerfil.DataEnvio
                } : null
            };
        }
    }
}