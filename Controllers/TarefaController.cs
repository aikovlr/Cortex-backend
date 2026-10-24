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
    public class TarefaController : ControllerBase
    {
        private readonly ITarefaService _tarefaService;
        private readonly IResponsavelTarefaService _responsavelTarefaService;
        private readonly ILogger<TarefaController> _logger;

        public TarefaController(
            ITarefaService tarefaService,
            IResponsavelTarefaService responsavelTarefaService,
            ILogger<TarefaController> logger)
        {
            _tarefaService = tarefaService;
            _responsavelTarefaService = responsavelTarefaService;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Criar([FromForm] CriarTarefaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var tarefa = await _tarefaService.CriarTarefaAsync(dto, currentUserId);

            _logger.LogInformation("Tarefa criada com sucesso: TarefaId {TarefaId}, CriadorId {CriadorId}",
                tarefa.Id, currentUserId);

            return CreatedAtAction(nameof(ObterPorId), new { id = tarefa.Id }, tarefa);
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] FiltroTarefasDTO filtros)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var tarefas = await _tarefaService.ListarTarefasAsync(filtros, currentUserId);
            return Ok(tarefas);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var tarefa = await _tarefaService.ObterTarefaPorIdAsync(id, currentUserId);

            if (tarefa == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Tarefa não encontrada",
                    Detail = $"Nenhuma tarefa foi encontrada com o ID {id}.",
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(tarefa);
        }

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Atualizar(int id, [FromForm] AtualizarTarefaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var tarefaAtualizada = await _tarefaService.AtualizarTarefaAsync(id, dto, currentUserId);

            _logger.LogInformation("Tarefa atualizada: TarefaId {TarefaId} por SolicitanteId {SolicitanteId}",
                id, currentUserId);

            return Ok(tarefaAtualizada);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deletar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var deletada = await _tarefaService.DeletarTarefaAsync(id, currentUserId);

            if (!deletada)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Tarefa não encontrada",
                    Detail = $"Nenhuma tarefa foi encontrada com o ID {id}.",
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogWarning("Tarefa excluída: TarefaId {TarefaId} por SolicitanteId {SolicitanteId}",
                id, currentUserId);

            return NoContent();
        }

        [HttpPatch("{id:int}/concluir")]
        public async Task<IActionResult> Concluir(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var tarefaConcluida = await _tarefaService.ConcluirTarefaAsync(id, currentUserId);

            _logger.LogInformation("Tarefa concluída: TarefaId {TarefaId} por SolicitanteId {SolicitanteId}",
                id, currentUserId);

            return Ok(tarefaConcluida);
        }

        [HttpPost("{id:int}/responsaveis")]
        public async Task<IActionResult> AtribuirResponsavel(int id, [FromBody] CriarResponsavelTarefaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            dto.TarefaId = id;

            var responsavel = await _responsavelTarefaService.AtribuirResponsavelAsync(dto, currentUserId);

            _logger.LogInformation("Responsável atribuído à TarefaId {TarefaId}: UsuarioId {UsuarioId}",
                id, dto.UsuarioId);

            return CreatedAtAction(nameof(ListarResponsaveis), new { id }, responsavel);
        }

        [HttpPost("{id:int}/responsaveis/equipe")]
        public async Task<IActionResult> AtribuirEquipe(int id, [FromBody] AtribuirEquipeTarefaDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var responsaveis = await _responsavelTarefaService.AtribuirEquipeAsync(id, dto.EquipeId, currentUserId);

            _logger.LogInformation("Equipe {EquipeId} atribuída à TarefaId {TarefaId}",
                dto.EquipeId, id);

            return Ok(responsaveis);
        }

        [HttpDelete("{id:int}/responsaveis/{respId:int}")]
        public async Task<IActionResult> RemoverResponsavel(int id, int respId)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            await _responsavelTarefaService.RemoverResponsavelAsync(respId, currentUserId);

            _logger.LogWarning("Responsável {RespId} removido da TarefaId {TarefaId} por SolicitanteId {SolicitanteId}",
                respId, id, currentUserId);

            return NoContent();
        }

        [HttpGet("{id:int}/responsaveis")]
        public async Task<IActionResult> ListarResponsaveis(int id)
        {
            var responsaveis = await _responsavelTarefaService.ListarResponsaveisPorTarefaAsync(id);
            return Ok(responsaveis);
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
