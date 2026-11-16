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

        [HttpGet("api/preferencias/acessibilidade")]
        [HttpGet("api/usuario/me/preferencias/acessibilidade")]
        public async Task<IActionResult> ObterPreferenciasAcessibilidade()
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var acessibilidade = await _notificacaoService.ObterPreferenciaAcessibilidadeAsync(currentUserId);
            return Ok(acessibilidade);
        }

        [HttpPut("api/preferencias/acessibilidade")]
        [HttpPut("api/usuario/me/preferencias/acessibilidade")]
        public async Task<IActionResult> AtualizarPreferenciasAcessibilidade([FromBody] AtualizarPreferenciaAcessibilidadeDTO dto)
        {
            var currentUserId = ObterUsuarioIdAutenticado();
            var acessibilidadeAtualizada = await _notificacaoService.AtualizarPreferenciaAcessibilidadeAsync(currentUserId, dto);
            return Ok(acessibilidadeAtualizada);
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

        [HttpPost("api/notificacoes/trigger-teste")]
        [AllowAnonymous]
        public async Task<IActionResult> TriggerTesteNotificacoes([FromQuery] int usuarioId = 1)
        {
            // Limpa supressão de disparo em memória para permitir re-trigger imediato dos testes
            Cortex.Services.Implementations.NotificacaoService.LimparHistoricoDisparos();

            var resultados = new List<object>();

            // 1. Resposta de tarefa individual: apenas 1 resposta, sem contador
            var n1 = await _notificacaoService.NotificarAsync(
                Cortex.Entities.ENUMS.TipoEventoNotificacao.NovaAtribuicao,
                usuarioId,
                "Nova Resposta Recebida",
                "Carlos Oliveira enviou uma resposta para a tarefa \"Finalizar testes E2E\".",
                "/tarefas-atribuidas/44?aba=respostas",
                destaque: false,
                tarefaId: 44,
                tipoInApp: "tarefa"
            );
            resultados.Add(n1);

            // 2. Resposta de tarefa com mais de uma notificação: agrupada com contador '2x'
            var n2 = await _notificacaoService.NotificarAsync(
                Cortex.Entities.ENUMS.TipoEventoNotificacao.NovaAtribuicao,
                usuarioId,
                "Nova Resposta Recebida (2)",
                "Dois membros enviaram respostas para a tarefa \"Atualizar documentação\".",
                "/tarefas-atribuidas/40?aba=respostas",
                destaque: false,
                tarefaId: 40,
                tipoInApp: "tarefa"
            );
            resultados.Add(n2);

            // 3. Notificação de equipe mencionando estritamente cargo de administrador
            var n3 = await _notificacaoService.NotificarAsync(
                Cortex.Entities.ENUMS.TipoEventoNotificacao.Equipe,
                usuarioId,
                "Entrada em Nova Equipe",
                "Você foi adicionado à equipe \"Engenharia de Plataforma\" como administrador.",
                "/equipes",
                destaque: false,
                tipoInApp: "equipe"
            );
            resultados.Add(n3);

            // 4. Notificação de ticket de reporte respondido marcando como resolvido
            var n4 = await _notificacaoService.NotificarAsync(
                Cortex.Entities.ENUMS.TipoEventoNotificacao.Ticket,
                usuarioId,
                "Ticket de Reporte Resolvido",
                "O ticket de reporte na tarefa \"Atualizar documentação\" foi marcado como resolvido.",
                "/tarefas-atribuidas/40?aba=tickets",
                destaque: false,
                tarefaId: 40,
                tipoInApp: "resposta_ticket"
            );
            resultados.Add(n4);

            return Ok(new { mensagem = "Notificações in-app e push disparadas com sucesso!", resultados });
        }
    }
}
