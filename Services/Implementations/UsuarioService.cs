using System.Security.Cryptography;
using Cortex.DTOs;
using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Gateways;
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
        private readonly IAnexoRepository? _anexoRepository;
        private readonly INotificacaoQueue? _notificacaoQueue;
        private readonly IEmailTemplateService? _emailTemplateService;

        public UsuarioService(
            IUsuarioRepository usuarioRepository, 
            IPasswordService passwordService,
            IArquivoValidacaoService arquivoValidacaoService,
            IWebHostEnvironment environment,
            ILogger<UsuarioService> logger,
            IAnexoRepository? anexoRepository = null,
            INotificacaoQueue? notificacaoQueue = null,
            IEmailTemplateService? emailTemplateService = null)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
            _arquivoValidacaoService = arquivoValidacaoService;
            _environment = environment;
            _logger = logger;
            _anexoRepository = anexoRepository;
            _notificacaoQueue = notificacaoQueue;
            _emailTemplateService = emailTemplateService;
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
                DataCriacao = DateTime.UtcNow,
                AceitouTermosPrivacidade = dto.AceitouTermosPrivacidade,
                DataAceiteTermos = dto.AceitouTermosPrivacidade ? DateTime.UtcNow : default
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

            if (dto.RemoverFoto && usuario.FotoPerfil != null)
            {
                await RemoverFotoPerfilInternoAsync(usuario);
            }
            else if (dto.FotoPerfil != null)
            {
                if (usuario.FotoPerfil != null)
                {
                    await RemoverFotoPerfilInternoAsync(usuario);
                }
                usuario.FotoPerfil = await SalvarFotoPerfilAsync(dto.FotoPerfil);
            }

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            return MapToConsultaDTO(usuario);
        }

        public async Task<bool> RemoverFotoPerfilAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
            if (usuario == null)
                return false;

            if (usuario.FotoPerfil != null)
            {
                await RemoverFotoPerfilInternoAsync(usuario);
                await _usuarioRepository.AtualizarUsuarioAsync(usuario);
            }

            return true;
        }

        private async Task RemoverFotoPerfilInternoAsync(Usuario usuario)
        {
            if (usuario.FotoPerfil == null) return;

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var caminhoArquivo = Path.Combine(webRoot, usuario.FotoPerfil.UrlCaminho.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(caminhoArquivo))
            {
                try { File.Delete(caminhoArquivo); } catch { /* Ignore lock */ }
            }

            if (_anexoRepository != null)
            {
                await _anexoRepository.RemoverAnexoAsync(usuario.FotoPerfil.Id);
            }
            usuario.FotoPerfil = null;
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
            usuario.SenhaAlteradaEm = DateTime.UtcNow;
            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            return true;
        }

        public async Task<bool> SolicitarRedefinicaoSenhaAsync(string email)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email);
            if (usuario == null)
            {
                // Resposta genérica constante para evitar enumeração de usuários
                return true;
            }

            // Cooldown de 60 segundos entre solicitações para a mesma conta
            var agoraUtc = DateTime.UtcNow;
            if (usuario.UltimaSolicitacaoRedefinicaoEm.HasValue &&
                usuario.UltimaSolicitacaoRedefinicaoEm.Value.AddSeconds(60) > agoraUtc)
            {
                _logger.LogInformation(
                    "Solicitação de redefinição de senha para {Email} ignorada devido ao cooldown de 60 segundos.",
                    LogEmailGateway.MascararEmail(email));
                return true;
            }

            // 6 dígitos numéricos gerados com RandomNumberGenerator (0 a 999999 formatado como D6)
            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            usuario.CodigoRedefinicaoHash = _passwordService.HashPassword(codigo);
            usuario.CodigoRedefinicaoExpiracao = agoraUtc.AddMinutes(15);
            usuario.TentativasRedefinicao = 0;
            usuario.UltimaSolicitacaoRedefinicaoEm = agoraUtc;

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            _logger.LogInformation(
                "Código de redefinição de senha gerado para UsuarioId {UsuarioId}. Expira em {Expiracao} UTC.",
                usuario.Id, usuario.CodigoRedefinicaoExpiracao);

            // Enfileira o e-mail na fila assíncrona (não espera SMTP)
            if (_emailTemplateService != null && _notificacaoQueue != null)
            {
                var emailMsg = _emailTemplateService.GerarCodigoRedefinicaoSenha(usuario.Email, usuario.Nome, codigo, 15);
                await _notificacaoQueue.EnfileirarAsync(new NotificacaoDespacho
                {
                    Canal = CanalNotificacao.Email,
                    UsuarioId = usuario.Id,
                    Destinatario = usuario.Email,
                    NomeDestinatario = usuario.Nome,
                    Assunto = emailMsg.Assunto,
                    MensagemHtml = emailMsg.CorpoHtml,
                    MensagemTexto = emailMsg.CorpoTexto ?? string.Empty,
                    TipoEvento = "redefinicao_senha"
                });
            }

            return true;
        }

        public async Task<bool> RedefinirSenhaComCodigoAsync(string email, string codigo, string novaSenha)
        {
            const string mensagemErroGenerico = "Código inválido ou expirado.";

            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email);
            if (usuario == null)
                throw new ArgumentException(mensagemErroGenerico);

            if (string.IsNullOrWhiteSpace(usuario.CodigoRedefinicaoHash) ||
                !usuario.CodigoRedefinicaoExpiracao.HasValue)
            {
                throw new ArgumentException(mensagemErroGenerico);
            }

            if (DateTime.UtcNow > usuario.CodigoRedefinicaoExpiracao.Value)
            {
                throw new ArgumentException(mensagemErroGenerico);
            }

            if (usuario.TentativasRedefinicao >= 5)
            {
                // Invalida o código após estourar 5 tentativas
                usuario.CodigoRedefinicaoHash = null;
                usuario.CodigoRedefinicaoExpiracao = null;
                await _usuarioRepository.AtualizarUsuarioAsync(usuario);
                throw new ArgumentException(mensagemErroGenerico);
            }

            // Valida os requisitos da nova senha ANTES de consumir a tentativa do código (RN02)
            if (_passwordService.VerifyPassword(novaSenha, usuario.SenhaHash))
                throw new ArgumentException("A nova senha não pode ser igual à senha atual.");

            var (isValid, errorMessage) = _passwordService.ValidatePassword(novaSenha);
            if (!isValid)
                throw new ArgumentException(errorMessage);

            // Valida o código com hash BCrypt
            var codigoValido = _passwordService.VerifyPassword(codigo, usuario.CodigoRedefinicaoHash);
            if (!codigoValido)
            {
                usuario.TentativasRedefinicao++;
                if (usuario.TentativasRedefinicao >= 5)
                {
                    usuario.CodigoRedefinicaoHash = null;
                    usuario.CodigoRedefinicaoExpiracao = null;
                }
                await _usuarioRepository.AtualizarUsuarioAsync(usuario);
                throw new ArgumentException(mensagemErroGenerico);
            }

            // Sucesso: atualiza senha e invalida dados do código
            usuario.SenhaHash = _passwordService.HashPassword(novaSenha);
            usuario.CodigoRedefinicaoHash = null;
            usuario.CodigoRedefinicaoExpiracao = null;
            usuario.TentativasRedefinicao = 0;
            usuario.SenhaAlteradaEm = DateTime.UtcNow;

            // Se o usuário conseguiu recuperar a senha via e-mail, confirmamos a posse do e-mail
            usuario.EmailVerificado = true;

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            _logger.LogInformation("Senha redefinida com sucesso para UsuarioId {UsuarioId}.", usuario.Id);

            // Envia e-mail assíncrono de confirmação
            if (_emailTemplateService != null && _notificacaoQueue != null)
            {
                var emailMsg = _emailTemplateService.GerarConfirmacaoSenhaAlterada(usuario.Email, usuario.Nome);
                await _notificacaoQueue.EnfileirarAsync(new NotificacaoDespacho
                {
                    Canal = CanalNotificacao.Email,
                    UsuarioId = usuario.Id,
                    Destinatario = usuario.Email,
                    NomeDestinatario = usuario.Nome,
                    Assunto = emailMsg.Assunto,
                    MensagemHtml = emailMsg.CorpoHtml,
                    MensagemTexto = emailMsg.CorpoTexto ?? string.Empty,
                    TipoEvento = "senha_alterada"
                });
            }

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
                Nivel = usuario.Nivel,
                Experiencia = usuario.Experiencia,
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
