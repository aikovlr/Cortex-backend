using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;

namespace Cortex.Services.Interfaces
{
    public interface INotificacaoService
    {
        Task<bool> EnviarNotificacaoAsync(int usuarioId, CanalNotificacao canal, string assunto, string mensagem);
        Task<PreferenciaNotificacaoDTO> ConfigurarPreferenciasAsync(int usuarioId, ConfigurarPreferenciaDTO dto);
        Task<IEnumerable<PreferenciaNotificacaoDTO>> ObterPreferenciasAsync(int usuarioId);
        Task<int> ProcessarAlertasVencimentoAsync();
    }
}
