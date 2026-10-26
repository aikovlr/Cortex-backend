using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.DTOs;
using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cortex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenService _tokenService;
        private readonly IVerificacaoEmailService _verificacaoEmailService;
        private readonly IEmailGateway _emailGateway;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUsuarioService usuarioService,
            IUsuarioRepository usuarioRepository,
            ITokenService tokenService,
            IVerificacaoEmailService verificacaoEmailService,
            IEmailGateway emailGateway,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _usuarioService = usuarioService;
            _usuarioRepository = usuarioRepository;
            _tokenService = tokenService;
            _verificacaoEmailService = verificacaoEmailService;
            _emailGateway = emailGateway;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> Login([FromBody] DadosLoginDTO dto)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Uma única mensagem para qualquer falha de credencial (previne user enumeration)
            const string mensagemErroGenerico = "E-mail ou senha incorretos.";

            var credenciaisValidas = await _usuarioService.ValidarSenhaAsync(dto.Email, dto.Senha);
            if (!credenciaisValidas)
            {
                _logger.LogWarning("Auditoria: Login falhado. Email: {Email}, IP: {IP}", dto.Email, clientIp);
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autorizado",
                    Detail = mensagemErroGenerico,
                    Instance = HttpContext.Request.Path
                });
            }

            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
            if (usuario == null)
            {
                _logger.LogError("Inconsistência: ValidarSenhaAsync retornou true mas usuário não encontrado. Email: {Email}", dto.Email);
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autorizado",
                    Detail = mensagemErroGenerico,
                    Instance = HttpContext.Request.Path
                });
            }

            if (!usuario.EmailVerificado)
            {
                _logger.LogWarning("Auditoria: Tentativa de login com e-mail não verificado. UserId: {UserId}, Email: {Email}, IP: {IP}",
                    usuario.Id, usuario.Email, clientIp);

                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "E-mail não verificado",
                    Detail = "O seu e-mail ainda não foi verificado. Por favor, confirme o código enviado para o seu e-mail antes de acessar a plataforma.",
                    Instance = HttpContext.Request.Path
                });
            }

            var equipePrincipal = usuario.Equipes.OrderBy(e => e.Cargo).FirstOrDefault();
            var cargo = equipePrincipal?.Cargo.ToString() ?? "Membro";
            var equipeId = equipePrincipal?.EquipeId;

            var token = _tokenService.GerarToken(usuario, cargo, equipeId);

            _logger.LogInformation("Auditoria: Login bem-sucedido. UserId: {UserId}, Email: {Email}, IP: {IP}, Timestamp: {Timestamp}",
                usuario.Id, usuario.Email, clientIp, DateTime.UtcNow);

            var expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var exp) ? exp : 1440;

            // Grava o token JWT em cookie HttpOnly + Secure (RNF01 / Padrão Obrigatório)
            Response.Cookies.Append("cortex_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
                Path = "/"
            });

            var response = new LoginResponseDTO
            {
                Token = token,
                Tipo = "Bearer",
                Usuario = new ConsultaUsuarioDTO
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    Telefone = usuario.Telefone,
                    FotoPerfil = usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = usuario.FotoPerfil.Id,
                        UrlCaminho = usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = usuario.FotoPerfil.NomeOriginal,
                        MimeType = usuario.FotoPerfil.MimeType,
                        DataEnvio = usuario.FotoPerfil.DataEnvio
                    } : null
                }
            };

            return Ok(response);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("cortex_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });

            return NoContent();
        }

        [HttpPost("alterar-senha")]
        [Authorize]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDTO dto)
        {
            var usuarioId = ObterUsuarioIdAutenticado();

            await _usuarioService.AlterarSenhaAsync(usuarioId, dto.SenhaAtual, dto.NovaSenha);

            _logger.LogInformation("Senha alterada com sucesso para UserId {UserId}", usuarioId);

            return NoContent();
        }

        [HttpPost("esqueci-senha")]
        [AllowAnonymous]
        [EnableRateLimiting("verificacao-email-policy")]
        public async Task<IActionResult> EsqueciSenha([FromBody] SolicitarRedefinicaoSenhaDTO dto)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
            if (usuario == null)
            {
                // Resposta genérica para evitar enumeração de usuários
                return Ok(new { message = "Se o e-mail informado estiver cadastrado, um código de recuperação foi enviado." });
            }

            var codigo = await _verificacaoEmailService.GerarEArmazenarCodigoAsync(usuario.Id);
            await _emailGateway.EnviarEmailAsync(dto.Email, "Recuperação de Senha - Cortex", $"Seu código para redefinição de senha é: {codigo}");

            _logger.LogInformation("Código de recuperação de senha enviado para UsuarioId {UserId}", usuario.Id);

            return Ok(new { message = "Se o e-mail informado estiver cadastrado, um código de recuperação foi enviado." });
        }

        [HttpPost("redefinir-senha")]
        [AllowAnonymous]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDTO dto)
        {
            await _usuarioService.RedefinirSenhaComCodigoAsync(dto.Email, dto.Codigo, dto.NovaSenha);

            _logger.LogInformation("Senha redefinida com sucesso para o e-mail {Email}", dto.Email);

            return Ok(new { message = "Senha redefinida com sucesso. Você já pode fazer login com a nova senha." });
        }

        [HttpPost("solicitar-verificacao-email")]
        [AllowAnonymous]
        [EnableRateLimiting("verificacao-email-policy")]
        public async Task<IActionResult> SolicitarCodigoVerificacao([FromBody] SolicitarCodigoVerificacaoDTO dto)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
            if (usuario == null)
                return Ok(new { message = "Se o e-mail existir, um código foi enviado." });

            var codigo = await _verificacaoEmailService.GerarEArmazenarCodigoAsync(usuario.Id);
            await _emailGateway.EnviarEmailAsync(dto.Email, "Verificação de e-mail", $"Seu código: {codigo}");

            return Ok(new { message = "Se o e-mail existir, um código foi enviado." });
        }

        [HttpPost("verificar-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarEmail([FromBody] VerificarEmailDTO dto)
        {
            await _verificacaoEmailService.VerificarCodigoAsync(dto.Email, dto.Codigo);
            return Ok(new { message = "E-mail verificado com sucesso." });
        }
    }
}
