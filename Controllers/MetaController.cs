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
    public class MetaController : ControllerBase
    {
        private readonly IMetaService _metaService;
        private readonly ILogger<MetaController> _logger;

        public MetaController(
            IMetaService metaService,
            ILogger<MetaController> logger)
        {
            _metaService = metaService;
            _logger = logger;
        }

        [HttpPost("api/metas")]
        public async Task<IActionResult> Criar([FromBody] CriarMetaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var meta = await _metaService.CriarMetaAsync(dto, currentUserId);

            _logger.LogInformation("Meta {MetaId} ('{Nome}') criada com sucesso pelo UsuarioId {UsuarioId}",
                meta.Id, meta.Nome, currentUserId);

            return CreatedAtAction(nameof(ObterPorId), new { id = meta.Id }, meta);
        }

        [HttpGet("api/metas")]
        public async Task<IActionResult> Listar([FromQuery] int? usuarioId)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var targetUsuarioId = usuarioId ?? currentUserId;

            var metas = await _metaService.ListarMetasPorUsuarioAsync(targetUsuarioId, currentUserId);
            return Ok(metas);
        }

        [HttpGet("api/metas/{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var meta = await _metaService.ObterMetaPorIdAsync(id, currentUserId);

            if (meta == null)
                return NotFound(new { message = "Meta não encontrada." });

            return Ok(meta);
        }

        [HttpPut("api/metas/{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarMetaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var metaAtualizada = await _metaService.AtualizarMetaAsync(id, dto, currentUserId);

            _logger.LogInformation("Meta {MetaId} atualizada com sucesso pelo UsuarioId {UsuarioId}",
                id, currentUserId);

            return Ok(metaAtualizada);
        }

        [HttpDelete("api/metas/{id:int}")]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var deletado = await _metaService.DeletarMetaAsync(id, currentUserId);

            if (!deletado)
                return NotFound(new { message = "Meta não encontrada." });

            _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                "Meta", id, currentUserId);

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
