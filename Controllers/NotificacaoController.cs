using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.DTOs.Request;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    [ApiController]
    [Authorize]
    public class NotificacaoController : ControllerBase
    {
        private readonly INotificacaoService _notificacaoService;
        private readonly ILogger<NotificacaoController> _logger;

        public NotificacaoController(
            INotificacaoService notificacaoService,
            ILogger<NotificacaoController> logger)
        {
            _notificacaoService = notificacaoService;
            _logger = logger;
        }

        [HttpGet("api/notificacoes/preferencias")]
        public async Task<IActionResult> ObterPreferencias()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var preferencias = await _notificacaoService.ObterPreferenciasAsync(currentUserId);
            return Ok(preferencias);
        }

        [HttpPut("api/notificacoes/preferencias")]
        public async Task<IActionResult> ConfigurarPreferencias([FromBody] ConfigurarPreferenciaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var preferenciaAtualizada = await _notificacaoService.ConfigurarPreferenciasAsync(currentUserId, dto);

            _logger.LogInformation("Preferência de notificação para o canal {Canal} atualizada pelo UsuarioId {UsuarioId}",
                dto.Canal, currentUserId);

            return Ok(preferenciaAtualizada);
        }

        private int ObterUsuarioIdAutenticado()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(claim, out var id))
                return id;

            throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido no token.");
        }
    }
}
