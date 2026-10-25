using Cortex.Entities;

namespace Cortex.Services.Interfaces
{
    /// <summary>
    /// Centraliza a lógica de autorização baseada em cargo dentro de equipes.
    /// Suporta usuários com múltiplos cargos em equipes distintas.
    /// </summary>
    public interface IEquipeAuthorizationService
    {
        /// <summary>Verifica se o usuário é Dono ou Admin na equipe especificada.</summary>
        Task<bool> EhAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>Verifica se o usuário é Dono na equipe especificada (RN03).</summary>
        Task<bool> EhDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>
        /// Verifica se o usuário é Dono ou Admin em pelo menos uma das equipes
        /// cujos membros são responsáveis pela tarefa informada.
        /// </summary>
        Task<bool> EhAdminOuDonoDaTarefaAsync(int usuarioId, Tarefa tarefa);

        /// <summary>Lança ForbiddenException se o usuário não for Admin/Dono da equipe.</summary>
        Task ExigirAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>Lança ForbiddenException se o usuário não for Dono da equipe.</summary>
        Task ExigirDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>Verifica se o solicitante é Dono ou Admin em alguma das equipes às quais o usuário alvo pertence.</summary>
        Task<bool> EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(int solicitanteId, int usuarioAlvoId);
    }
}
