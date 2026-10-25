using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IPreferenciaNotificacaoRepository
    {
        Task<PreferenciaNotificacao?> ObterPorUsuarioECanalAsync(int usuarioId, CanalNotificacao canal);
        Task<List<PreferenciaNotificacao>> ObterPorUsuarioAsync(int usuarioId);
        Task<PreferenciaNotificacao> AdicionarAsync(PreferenciaNotificacao preferencia);
        Task<PreferenciaNotificacao> AtualizarAsync(PreferenciaNotificacao preferencia);
    }
}
