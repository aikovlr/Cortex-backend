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
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<RelatorioController> _logger;

        public RelatorioController(
            IRelatorioService relatorioService,
            ILogger<RelatorioController> logger)
        {
            _relatorioService = relatorioService;
            _logger = logger;
        }

        [HttpGet("api/relatorios/tarefas")]
        public async Task<IActionResult> ObterRelatorioTarefas([FromQuery] FiltroRelatorioDTO filtro)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var relatorio = await _relatorioService.GerarRelatorioTarefasAsync(filtro, currentUserId);
            return Ok(relatorio);
        }

        [HttpGet("api/relatorios/equipes/{id:int}")]
        public async Task<IActionResult> ObterRelatorioEquipe(int id, [FromQuery] FiltroRelatorioDTO filtro)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var relatorioEquipe = await _relatorioService.GerarRelatorioEquipeAsync(id, filtro, currentUserId);
            return Ok(relatorioEquipe);
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
