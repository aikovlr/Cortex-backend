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
    public class MetaController : BaseController
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

        [HttpPost("api/tarefas/{id:int}/metas")]
        public async Task<IActionResult> CriarPorTarefa(int id, [FromBody] CriarMetaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            dto.TarefaId = id;

            var meta = await _metaService.CriarMetaAsync(dto, currentUserId);

            _logger.LogInformation("Meta {MetaId} ('{Nome}') criada para TarefaId {TarefaId} pelo UsuarioId {UsuarioId}",
                meta.Id, meta.Nome, id, currentUserId);

            return CreatedAtAction(nameof(ObterPorId), new { id = meta.Id }, meta);
        }

        [HttpGet("api/metas")]
        public async Task<IActionResult> Listar([FromQuery] int? usuarioId)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var targetUsuarioId = usuarioId ?? currentUserId;

            // Garante que metas expiradas sejam verificadas e tenham penalidade de XP aplicada
            await _metaService.VerificarMetasExpiradasAsync();

            var metas = await _metaService.ListarMetasPorUsuarioAsync(targetUsuarioId, currentUserId);
            return Ok(metas);
        }

        [HttpGet("api/tarefas/{id:int}/metas")]
        public async Task<IActionResult> ListarPorTarefa(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var metas = await _metaService.ListarMetasPorTarefaAsync(id, currentUserId);
            return Ok(metas);
        }

        [HttpGet("api/metas/{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var meta = await _metaService.ObterMetaPorIdAsync(id, currentUserId);

            if (meta == null)
                return NaoEncontrado("Meta não encontrada.", "Meta não encontrada");

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
                return NaoEncontrado("Meta não encontrada.", "Meta não encontrada");

            _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                "Meta", id, currentUserId);

            return NoContent();
        }

        [HttpPost("api/metas/{id:int}/equipe")]
        public async Task<IActionResult> AtribuirEquipe(int id, [FromBody] AtribuirEquipeMetaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var metaAtualizada = await _metaService.AtribuirEquipeAsync(id, dto.EquipeId, currentUserId, dto.XpRecompensa);

            _logger.LogInformation("Equipe {EquipeId} atribuída à Meta {MetaId} pelo UsuarioId {UsuarioId}",
                dto.EquipeId, id, currentUserId);

            return Ok(metaAtualizada);
        }

        [HttpPost("api/metas/verificar-expiradas")]
        public async Task<IActionResult> VerificarMetasExpiradas()
        {
            var total = await _metaService.VerificarMetasExpiradasAsync();
            _logger.LogInformation("Verificação manual de metas expiradas executada. Total penalizadas: {Total}", total);
            return Ok(new { metasPenalizadas = total });
        }

        [HttpGet("api/equipes/{id:int}/metas")]
        public async Task<IActionResult> ListarPorEquipe(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var metas = await _metaService.ListarMetasPorEquipeAsync(id, currentUserId);
            return Ok(metas);
        }
    }
}
