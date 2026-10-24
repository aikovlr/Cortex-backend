using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.DTOs;
using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUsuarioService usuarioService,
            IUsuarioRepository usuarioRepository,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _usuarioService = usuarioService;
            _usuarioRepository = usuarioRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] DadosLoginDTO dto)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var credenciaisValidas = await _usuarioService.ValidarSenhaAsync(dto.Email, dto.Senha);
            if (!credenciaisValidas)
            {
                _logger.LogWarning("Auditoria: Tentativa de login falhada. Email: {Email}, IP: {IP}, Timestamp: {Timestamp}",
                    dto.Email, clientIp, DateTime.UtcNow);

                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autorizado",
                    Detail = "E-mail ou senha incorretos.",
                    Instance = HttpContext.Request.Path
                });
            }

            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
            if (usuario == null)
            {
                _logger.LogWarning("Auditoria: Tentativa de login falhada. Email: {Email}, IP: {IP}, Timestamp: {Timestamp}",
                    dto.Email, clientIp, DateTime.UtcNow);

                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autorizado",
                    Detail = "Usuário não encontrado.",
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
    }
}
