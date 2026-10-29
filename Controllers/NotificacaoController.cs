using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.DTOs.Request;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    [ApiController]
    [Route("")]
    [Authorize]
    public class NotificacaoController : BaseController
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

        [HttpGet("api/notificacoes")]
        public async Task<IActionResult> ObterNotificacoes()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var notificacoes = await _notificacaoService.ObterNotificacoesUsuarioAsync(currentUserId);
            return Ok(notificacoes);
        }

        [HttpPut("api/notificacoes/{id:int}/lida")]
        public async Task<IActionResult> MarcarComoLida(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var sucesso = await _notificacaoService.MarcarComoLidaAsync(id, currentUserId);
            if (!sucesso)
            {
                return NaoEncontrado("Notificação não encontrada.", "Notificação não encontrada");
            }
            return NoContent();
        }

        [HttpPut("api/notificacoes/marcar-todas-lidas")]
        public async Task<IActionResult> MarcarTodasComoLidas()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            await _notificacaoService.MarcarTodasComoLidasAsync(currentUserId);
            return NoContent();
        }

        [HttpDelete("api/notificacoes")]
        public async Task<IActionResult> LimparNotificacoes()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            await _notificacaoService.LimparNotificacoesAsync(currentUserId);
            return NoContent();
        }

        [HttpDelete("api/notificacoes/{id:int}")]
        public async Task<IActionResult> RemoverNotificacao(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var removido = await _notificacaoService.RemoverNotificacaoAsync(id, currentUserId);
            if (!removido)
            {
                return NaoEncontrado("Notificação não encontrada.", "Notificação não encontrada");
            }
            return NoContent();
        }
    }
}
