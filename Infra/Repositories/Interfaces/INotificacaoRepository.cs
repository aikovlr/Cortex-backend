using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface INotificacaoRepository
    {
        Task<Notificacao> AdicionarAsync(Notificacao notificacao);
        Task<IEnumerable<Notificacao>> ObterPorUsuarioAsync(int usuarioId);
        Task<Notificacao?> ObterPorIdAsync(int id);
        Task AtualizarAsync(Notificacao notificacao);
        Task MarcarTodasComoLidasAsync(int usuarioId);
        Task<bool> ExisteNotificacaoAsync(int usuarioId, string tipo, int tarefaId);
        Task LimparNotificacoesAsync(int usuarioId);
        Task<bool> RemoverNotificacaoAsync(int id, int usuarioId);
    }
}
