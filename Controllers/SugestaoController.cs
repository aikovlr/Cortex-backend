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
    public class SugestaoController : ControllerBase
    {
        private readonly ISugestaoService _sugestaoService;
        private readonly ILogger<SugestaoController> _logger;

        public SugestaoController(
            ISugestaoService sugestaoService,
            ILogger<SugestaoController> logger)
        {
            _sugestaoService = sugestaoService;
            _logger = logger;
        }

        [HttpPost("api/tarefas/{id:int}/sugestoes")]
        public async Task<IActionResult> Criar(int id, [FromBody] CriarSugestaoDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            dto.TarefaId = id;

            var sugestao = await _sugestaoService.CriarSugestaoAsync(dto, currentUserId);

            _logger.LogInformation("Sugestão {SugestaoId} submetida para a TarefaId {TarefaId} por UsuarioId {UsuarioId}",
                sugestao.Id, id, currentUserId);

            return CreatedAtAction(nameof(ListarPorTarefa), new { id }, sugestao);
        }

        [HttpGet("api/tarefas/{id:int}/sugestoes")]
        public async Task<IActionResult> ListarPorTarefa(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var sugestoes = await _sugestaoService.ListarSugestoesPorTarefaAsync(id, currentUserId);
            return Ok(sugestoes);
        }

        [HttpPatch("api/sugestoes/{id:int}/aprovar")]
        public async Task<IActionResult> Aprovar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var sugestao = await _sugestaoService.AprovarSugestaoAsync(id, currentUserId);

            _logger.LogInformation("Sugestão {SugestaoId} aprovada pelo criador {CriadorId}",
                id, currentUserId);

            return Ok(sugestao);
        }

        [HttpPatch("api/sugestoes/{id:int}/rejeitar")]
        public async Task<IActionResult> Rejeitar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var sugestao = await _sugestaoService.RejeitarSugestaoAsync(id, currentUserId);

            _logger.LogInformation("Sugestão {SugestaoId} rejeitada pelo criador {CriadorId}",
                id, currentUserId);

            return Ok(sugestao);
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
