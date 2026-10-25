using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IAlertaEnviadoRepository
    {
        Task<bool> ExisteAlertaEnviadoAsync(int usuarioId, int tarefaId, CanalNotificacao canal, DateTime desdeUtc);
        Task RegistrarAsync(int usuarioId, int tarefaId, CanalNotificacao canal);
    }
}
