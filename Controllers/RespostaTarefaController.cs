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
    public class RespostaTarefaController : ControllerBase
    {
        private readonly IRespostaTarefaService _respostaService;
        private readonly ILogger<RespostaTarefaController> _logger;

        public RespostaTarefaController(
            IRespostaTarefaService respostaService,
            ILogger<RespostaTarefaController> logger)
        {
            _respostaService = respostaService;
            _logger = logger;
        }

        [HttpPost("api/tarefas/{id:int}/respostas")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Criar(int id, [FromForm] CriarRespostaTarefaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            dto.TarefaId = id;

            var resposta = await _respostaService.CriarRespostaAsync(dto, currentUserId);

            _logger.LogInformation("Resposta {RespostaId} criada para TarefaId {TarefaId} por UsuarioId {UsuarioId}",
                resposta.Id, id, currentUserId);

            return CreatedAtAction(nameof(ListarPorTarefa), new { id }, resposta);
        }

        [HttpGet("api/tarefas/{id:int}/respostas")]
        public async Task<IActionResult> ListarPorTarefa(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var respostas = await _respostaService.ListarRespostasPorTarefaAsync(id, currentUserId);
            return Ok(respostas);
        }

        [HttpDelete("api/respostas/{id:int}")]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var deletado = await _respostaService.DeletarRespostaAsync(id, currentUserId);

            if (!deletado)
                return NotFound(new { message = "Resposta não encontrada." });

            _logger.LogWarning("Resposta {RespostaId} excluída por UsuarioId {UsuarioId}",
                id, currentUserId);

            return NoContent();
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
