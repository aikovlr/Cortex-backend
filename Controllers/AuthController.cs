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
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenService _tokenService;
        private readonly IVerificacaoEmailService _verificacaoEmailService;
        private readonly IEmailGateway _emailGateway;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUsuarioService usuarioService,
            IUsuarioRepository usuarioRepository,
            ITokenService tokenService,
            IVerificacaoEmailService verificacaoEmailService,
            IEmailGateway emailGateway,
            ILogger<AuthController> logger)
        {
            _usuarioService = usuarioService;
            _usuarioRepository = usuarioRepository;
            _tokenService = tokenService;
            _verificacaoEmailService = verificacaoEmailService;
            _emailGateway = emailGateway;
            _logger = logger;
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

            var equipePrincipal = usuario.Equipes.OrderBy(e => e.Cargo).FirstOrDefault();
            var cargo = equipePrincipal?.Cargo.ToString() ?? "Membro";
            var equipeId = equipePrincipal?.EquipeId;

            var token = _tokenService.GerarToken(usuario, cargo, equipeId);

            _logger.LogInformation("Auditoria: Login bem-sucedido. UserId: {UserId}, Email: {Email}, IP: {IP}, Timestamp: {Timestamp}",
                usuario.Id, usuario.Email, clientIp, DateTime.UtcNow);

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

        [HttpPost("alterar-senha")]
        [Authorize]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!int.TryParse(userIdClaim, out var usuarioId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autorizado",
                    Detail = "Identificador de usuário inválido no token.",
                    Instance = HttpContext.Request.Path
                });
            }

            await _usuarioService.AlterarSenhaAsync(usuarioId, dto.SenhaAtual, dto.NovaSenha);

            _logger.LogInformation("Senha alterada com sucesso para UserId {UserId}", usuarioId);

            return NoContent();
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
