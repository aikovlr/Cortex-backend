using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.Entities;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    [ApiController]
    [Authorize]
    public class AnexoController : ControllerBase
    {
        private readonly IAnexoService _anexoService;
        private readonly ILogger<AnexoController> _logger;

        public AnexoController(
            IAnexoService anexoService,
            ILogger<AnexoController> logger)
        {
            _anexoService = anexoService;
            _logger = logger;
        }

        [HttpPost("api/tarefas/{id:int}/anexos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAnexosTarefa(int id, [FromForm] List<IFormFile> arquivos)
        {
            var currentUserId = ObterUsuarioIdAutenticado();

            if (arquivos == null || !arquivos.Any())
                return BadRequest(new { message = "Nenhum arquivo enviado para upload." });

            var anexos = await _anexoService.AdicionarAnexosTarefaAsync(id, arquivos, currentUserId);

            _logger.LogInformation("Upload de {Count} anexo(s) realizado na TarefaId {TarefaId} por UsuarioId {UsuarioId}",
                anexos.Count(), id, currentUserId);

            return CreatedAtAction(nameof(ListarAnexosPorTarefa), new { id }, anexos);
        }

        [HttpGet("api/tarefas/{id:int}/anexos")]
        public async Task<IActionResult> ListarAnexosPorTarefa(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var anexos = await _anexoService.ListarAnexosPorTarefaAsync(id, currentUserId);
            return Ok(anexos);
        }

        [HttpGet("api/anexos/{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var anexo = await _anexoService.ObterAnexoPorIdAsync(id);
            if (anexo == null)
                return NotFound(new { message = "Anexo não encontrado." });

            return Ok(anexo);
        }

        [HttpGet("api/entidades/{tipoEntidade}/anexos/{entidadeId:int}")]
        public async Task<IActionResult> ListarPorEntidade(TipoEntidade tipoEntidade, int entidadeId)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var anexos = await _anexoService.ListarAnexosPorEntidadeAsync(tipoEntidade, entidadeId, currentUserId);
            return Ok(anexos);
        }

        [HttpDelete("api/anexos/{id:int}")]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var deletado = await _anexoService.DeletarAnexoAsync(id, currentUserId);

            if (!deletado)
                return NotFound(new { message = "Anexo não encontrado." });

            _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                "Anexo", id, currentUserId);

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
