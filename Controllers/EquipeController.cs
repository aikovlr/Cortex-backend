using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.DTOs.Request;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EquipeController : BaseController
    {
        private readonly IEquipeService _equipeService;
        private readonly IMembroEquipeService _membroEquipeService;
        private readonly ILogger<EquipeController> _logger;

        public EquipeController(
            IEquipeService equipeService,
            IMembroEquipeService membroEquipeService,
            ILogger<EquipeController> logger)
        {
            _equipeService = equipeService;
            _membroEquipeService = membroEquipeService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarEquipeDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var equipe = await _equipeService.CriarEquipeAsync(dto, currentUserId);

            _logger.LogInformation("Equipe criada com sucesso: EquipeId {EquipeId}, CriadorId {CriadorId}", 
                equipe.Id, currentUserId);

            return CreatedAtAction(nameof(ObterPorId), new { id = equipe.Id }, equipe);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var equipe = await _equipeService.ObterEquipePorIdAsync(id, currentUserId);

            if (equipe == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Equipe não encontrada",
                    Detail = $"Nenhuma equipe foi encontrada com o ID {id}.",
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(equipe);
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var equipes = await _equipeService.ListarEquipesAsync(currentUserId);
            return Ok(equipes);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarEquipeDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var equipeAtualizada = await _equipeService.AtualizarEquipeAsync(id, dto, currentUserId);

            _logger.LogInformation("Equipe atualizada: EquipeId {EquipeId} por SolicitanteId {SolicitanteId}", 
                id, currentUserId);

            return Ok(equipeAtualizada);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var removida = await _equipeService.DeletarEquipeAsync(id, currentUserId);

            if (!removida)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Equipe não encontrada",
                    Detail = $"Nenhuma equipe foi encontrada com o ID {id}.",
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogWarning("Equipe excluída: EquipeId {EquipeId} por SolicitanteId {SolicitanteId}", 
                id, currentUserId);

            return NoContent();
        }

        [HttpPost("{id:int}/membros")]
        public async Task<IActionResult> AdicionarMembro(int id, [FromBody] CriarMembroEquipeDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            dto.EquipeId = id;

            var novoMembro = await _membroEquipeService.AdicionarMembroAsync(dto, currentUserId);

            _logger.LogInformation("Membro adicionado à equipe {EquipeId}: UsuarioId {UsuarioId}, Cargo {Cargo} por SolicitanteId {SolicitanteId}", 
                id, dto.UsuarioId, dto.Cargo, currentUserId);

            return CreatedAtAction(nameof(ListarMembros), new { id }, novoMembro);
        }

        [HttpDelete("{id:int}/membros/{membroId:int}")]
        public async Task<IActionResult> RemoverMembro(int id, int membroId)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            await _membroEquipeService.RemoverMembroAsync(membroId, currentUserId);

            _logger.LogWarning("Membro removido da equipe {EquipeId}: MembroId {MembroId} por SolicitanteId {SolicitanteId}", 
                id, membroId, currentUserId);

            return NoContent();
        }

        [HttpPatch("{id:int}/membros/{membroId:int}/cargo")]
        public async Task<IActionResult> AlterarCargo(int id, int membroId, [FromBody] AtualizarMembroEquipeDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var membroAtualizado = await _membroEquipeService.AlterarCargoAsync(membroId, dto.Cargo, currentUserId);

            _logger.LogInformation("Cargo alterado na equipe {EquipeId}: MembroId {MembroId}, NovoCargo {Cargo} por SolicitanteId {SolicitanteId}", 
                id, membroId, dto.Cargo, currentUserId);

            return Ok(membroAtualizado);
        }

        [HttpGet("{id:int}/membros")]
        public async Task<IActionResult> ListarMembros(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var membros = await _membroEquipeService.ListarMembrosPorEquipeAsync(id, currentUserId);
            return Ok(membros);
        }
    }
}
