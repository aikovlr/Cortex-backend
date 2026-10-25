using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class EquipeAuthorizationService : IEquipeAuthorizationService
    {
        private readonly IMembroEquipeRepository _membroEquipeRepository;

        public EquipeAuthorizationService(IMembroEquipeRepository membroEquipeRepository)
        {
            _membroEquipeRepository = membroEquipeRepository;
        }

        public async Task<bool> EhAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, usuarioId);
            return membro != null && (membro.Cargo == Cargo.Dono || membro.Cargo == Cargo.Admin);
        }

        public async Task<bool> EhDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, usuarioId);
            return membro?.Cargo == Cargo.Dono;
        }

        public async Task<bool> EhAdminOuDonoDaTarefaAsync(int usuarioId, Tarefa tarefa)
        {
            if (tarefa.ResponsaveisTarefas == null)
                return false;

            // Encontra as equipes dos responsáveis pela tarefa
            var responsaveisIds = tarefa.ResponsaveisTarefas
                .Select(rt => rt.UsuarioId)
                .Distinct()
                .ToList();

            if (!responsaveisIds.Any())
                return false;

            // Busca as equipes em que os responsáveis estão como membros
            var equipesDosResponsaveis = await _membroEquipeRepository
                .ObterMembrosPorUsuarioEEquipesIdsAsync(responsaveisIds);

            var equipeIds = equipesDosResponsaveis.Select(m => m.EquipeId).Distinct().ToList();

            if (!equipeIds.Any())
                return false;

            // Verifica se o solicitante é Admin/Dono em alguma dessas equipes
            var membrosDoSolicitante = await _membroEquipeRepository
                .ObterMembrosPorUsuarioEEquipesAsync(usuarioId, equipeIds);

            return membrosDoSolicitante.Any(m => m.Cargo == Cargo.Dono || m.Cargo == Cargo.Admin);
        }

        public async Task ExigirAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            if (!await EhAdminOuDonoDaEquipeAsync(usuarioId, equipeId))
                throw new ForbiddenException("Apenas o Dono ou Administrador da equipe possuem permissão para esta operação.");
        }

        public async Task ExigirDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            if (!await EhDonoDaEquipeAsync(usuarioId, equipeId))
                throw new ForbiddenException("Apenas o Dono da equipe possui permissão para esta operação.");
        }

        public async Task<bool> EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(int solicitanteId, int usuarioAlvoId)
        {
            if (solicitanteId == usuarioAlvoId)
                return true;

            var membrosAlvo = await _membroEquipeRepository
                .ObterMembrosPorUsuarioEEquipesIdsAsync(new[] { usuarioAlvoId });

            var equipeIds = membrosAlvo.Select(m => m.EquipeId).Distinct().ToList();
            if (!equipeIds.Any())
                return false;

            var membrosSolicitante = await _membroEquipeRepository
                .ObterMembrosPorUsuarioEEquipesAsync(solicitanteId, equipeIds);

            return membrosSolicitante.Any(m => m.Cargo == Cargo.Dono || m.Cargo == Cargo.Admin);
        }
    }
}
