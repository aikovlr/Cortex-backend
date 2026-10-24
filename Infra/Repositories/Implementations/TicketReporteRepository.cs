using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class TicketReporteRepository : ITicketReporteRepository
    {
        private readonly CortexDbContext _context;

        public TicketReporteRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<TicketReporte?> ObterTicketReportePorIdAsync(int id)
        {
            return await _context.TicketReporte
                .Include(tr => tr.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .Include(tr => tr.Tarefa)
                    .ThenInclude(t => t.Criador)
                .Include(tr => tr.Tarefa)
                    .ThenInclude(t => t.ResponsaveisTarefas)
                .FirstOrDefaultAsync(tr => tr.Id == id);
        }

        public async Task<List<TicketReporte>> ObterTicketReportesPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.TicketReporte
                .Where(tr => tr.UsuarioId == usuarioId)
                .Include(tr => tr.Tarefa)
                .Include(tr => tr.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .OrderByDescending(tr => tr.DataCriacao)
                .ToListAsync();
        }
        
        public async Task<List<TicketReporte>> ObterTicketReportesPorTarefaIdAsync(int tarefaId)
        {
            return await _context.TicketReporte
                .Where(tr => tr.TarefaId == tarefaId)
                .Include(tr => tr.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .Include(tr => tr.Tarefa)
                .OrderByDescending(tr => tr.DataCriacao)
                .ToListAsync();
        }

        public async Task<List<TicketReporte>> ObterTicketsAbertosPorEquipeIdAsync(int equipeId)
        {
            var membrosIds = await _context.MembrosEquipe
                .Where(me => me.EquipeId == equipeId)
                .Select(me => me.UsuarioId)
                .ToListAsync();

            return await _context.TicketReporte
                .Where(tr => tr.Ativo && (membrosIds.Contains(tr.UsuarioId) 
                                       || membrosIds.Contains(tr.Tarefa.CriadorId) 
                                       || tr.Tarefa.ResponsaveisTarefas.Any(rt => membrosIds.Contains(rt.UsuarioId))))
                .Include(tr => tr.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .Include(tr => tr.Tarefa)
                .OrderByDescending(tr => tr.DataCriacao)
                .ToListAsync();
        }

        public async Task<TicketReporte> AdicionarTicketReporteAsync(TicketReporte ticketReporte)
        {
            _context.TicketReporte.Add(ticketReporte);
            await _context.SaveChangesAsync();
            return ticketReporte;
        }

        public async Task<TicketReporte> AtualizarTicketReporteAsync(TicketReporte ticketReporte)
        {
            _context.TicketReporte.Update(ticketReporte);
            await _context.SaveChangesAsync();
            return ticketReporte;
        }

        public async Task<bool> RemoverTicketReporteAsync(int id)
        {
            var ticketReporte = await _context.TicketReporte.FindAsync(id);
            if (ticketReporte == null) return false;

            _context.TicketReporte.Remove(ticketReporte);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TicketReporteExisteAsync(int id)
        {
            return await _context.TicketReporte.AnyAsync(tr => tr.Id == id);
        }
    }
}