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
    public class TicketReporteController : BaseController
    {
        private readonly ITicketReporteService _ticketReporteService;
        private readonly ILogger<TicketReporteController> _logger;

        public TicketReporteController(
            ITicketReporteService ticketReporteService,
            ILogger<TicketReporteController> logger)
        {
            _ticketReporteService = ticketReporteService;
            _logger = logger;
        }

        [HttpPost("api/tarefas/{id:int}/tickets")]
        public async Task<IActionResult> Criar(int id, [FromBody] CriarTicketReporteDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            dto.TarefaId = id;

            var ticket = await _ticketReporteService.CriarTicketAsync(dto, currentUserId);

            _logger.LogInformation("Auditoria: Abertura de ticket. TicketId: {TicketId}, TarefaId: {TarefaId}, UsuarioId: {UsuarioId}",
                ticket.Id, id, currentUserId);

            return CreatedAtAction(nameof(ListarPorTarefa), new { id }, ticket);
        }

        [HttpGet("api/tarefas/{id:int}/tickets")]
        public async Task<IActionResult> ListarPorTarefa(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var tickets = await _ticketReporteService.ListarTicketsPorTarefaAsync(id, currentUserId);
            return Ok(tickets);
        }

        [HttpPatch("api/tickets/{id:int}/fechar")]
        public async Task<IActionResult> Fechar(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var ticketFechado = await _ticketReporteService.FecharTicketAsync(id, currentUserId);

            _logger.LogInformation("Ticket de reporte {TicketId} fechado por SolicitanteId {SolicitanteId}",
                id, currentUserId);

            return Ok(ticketFechado);
        }

        [HttpGet("api/equipes/{id:int}/tickets")]
        public async Task<IActionResult> ListarAbertosPorEquipe(int id)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var ticketsAbertos = await _ticketReporteService.ListarTicketsAbertosAsync(id, currentUserId);
            return Ok(ticketsAbertos);
        }
    }
}
