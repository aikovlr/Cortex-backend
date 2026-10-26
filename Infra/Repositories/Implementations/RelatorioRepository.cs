using Cortex.DTOs.Request;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories.Implementations
{
    public class RelatorioRepository : IRelatorioRepository
    {
        private readonly CortexDbContext _context;

        public RelatorioRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tarefa>> ObterTarefasParaRelatorioAsync(
            int? solicitanteId,
            bool ehAdminOuDono,
            FiltroRelatorioDTO filtro)
        {
            var query = _context.Tarefas
                .AsNoTracking()
                .Include(t => t.ResponsaveisTarefas)
                .AsQueryable();

            if (filtro.CriadorId.HasValue)
            {
                query = query.Where(t => t.CriadorId == filtro.CriadorId.Value);
            }
            else if (filtro.ResponsavelId.HasValue)
            {
                query = query.Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == filtro.ResponsavelId.Value));
            }
            else if (filtro.EquipeId.HasValue)
            {
                var membrosEquipeIds = await _context.MembrosEquipe
                    .Where(m => m.EquipeId == filtro.EquipeId.Value)
                    .Select(m => m.UsuarioId)
                    .ToListAsync();

                query = query.Where(t => t.ResponsaveisTarefas.Any(rt => membrosEquipeIds.Contains(rt.UsuarioId)));
            }
            else if (solicitanteId.HasValue)
            {
                // Padrão: apenas as tarefas nas quais o usuário é responsável
                query = query.Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId.Value));
            }

            if (filtro.Status.HasValue)
                query = query.Where(t => t.Status == filtro.Status.Value);

            if (filtro.Prioridade.HasValue)
                query = query.Where(t => t.Prioridade == filtro.Prioridade.Value);

            if (filtro.DataInicio.HasValue)
            {
                var inicioUtc = filtro.DataInicio.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataInicio.Value 
                    : filtro.DataInicio.Value.ToUniversalTime();
                query = query.Where(t => t.DataVencimento >= inicioUtc);
            }

            if (filtro.DataFim.HasValue)
            {
                var fimUtc = filtro.DataFim.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataFim.Value 
                    : filtro.DataFim.Value.ToUniversalTime();
                query = query.Where(t => t.DataVencimento <= fimUtc);
            }

            return await query.ToListAsync();
        }

        public async Task<List<int>> ObterIdsMembrosDaEquipeAsync(int equipeId)
        {
            return await _context.MembrosEquipe
                .Where(m => m.EquipeId == equipeId)
                .Select(m => m.UsuarioId)
                .ToListAsync();
        }

        public async Task<List<Tarefa>> ObterTarefasDaEquipeAsync(int equipeId, FiltroRelatorioDTO filtro)
        {
            var membroIds = await ObterIdsMembrosDaEquipeAsync(equipeId);

            var query = _context.Tarefas
                .AsNoTracking()
                .Include(t => t.ResponsaveisTarefas)
                .Where(t => t.ResponsaveisTarefas.Any(rt => membroIds.Contains(rt.UsuarioId)));

            if (filtro.Status.HasValue)
                query = query.Where(t => t.Status == filtro.Status.Value);

            if (filtro.Prioridade.HasValue)
                query = query.Where(t => t.Prioridade == filtro.Prioridade.Value);

            if (filtro.DataInicio.HasValue)
            {
                var inicioUtc = filtro.DataInicio.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataInicio.Value 
                    : filtro.DataInicio.Value.ToUniversalTime();
                query = query.Where(t => t.DataVencimento >= inicioUtc);
            }

            if (filtro.DataFim.HasValue)
            {
                var fimUtc = filtro.DataFim.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataFim.Value 
                    : filtro.DataFim.Value.ToUniversalTime();
                query = query.Where(t => t.DataVencimento <= fimUtc);
            }

            return await query.ToListAsync();
        }
    }
}
