using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Entities.ENUMS;

namespace Cortex.Services.Interfaces
{
    public interface INotificacaoService
    {
        // Orquestrador Central de Notificações
        Task<NotificacaoDTO> NotificarAsync(
            TipoEventoNotificacao evento,
            int usuarioId,
            string titulo,
            string mensagem,
            string? link = null,
            bool destaque = false,
            int? tarefaId = null,
            string? tipoInApp = null);

        Task<bool> EnviarNotificacaoAsync(int usuarioId, CanalNotificacao canal, string assunto, string mensagem);
        Task<PreferenciaNotificacaoDTO> ConfigurarPreferenciasAsync(int usuarioId, ConfigurarPreferenciaDTO dto);
        Task<IEnumerable<PreferenciaNotificacaoDTO>> ObterPreferenciasAsync(int usuarioId);
        Task<PreferenciaAcessibilidadeDTO> ObterPreferenciaAcessibilidadeAsync(int usuarioId);
        Task<PreferenciaAcessibilidadeDTO> AtualizarPreferenciaAcessibilidadeAsync(int usuarioId, AtualizarPreferenciaAcessibilidadeDTO dto);
        Task<int> ProcessarAlertasVencimentoAsync();

        // Notificações persistidas no banco de dados
        Task<NotificacaoDTO> CriarNotificacaoAsync(int usuarioId, string tipo, string titulo, string mensagem, string link, bool destaque, int? tarefaId = null);
        Task<IEnumerable<NotificacaoDTO>> ObterNotificacoesUsuarioAsync(int usuarioId);
        Task<bool> MarcarComoLidaAsync(int notificacaoId, int usuarioId);
        Task<bool> MarcarTodasComoLidasAsync(int usuarioId);
        Task<bool> LimparNotificacoesAsync(int usuarioId);
        Task<bool> RemoverNotificacaoAsync(int notificacaoId, int usuarioId);
    }
}
