using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class ResponsavelTarefaRepository : IResponsavelTarefaRepository
    {
        private readonly CortexDbContext _context;

        public ResponsavelTarefaRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<ResponsavelTarefa?> ObterResponsavelTarefaPorIdAsync(int id)
        {
            return await _context.ResponsaveisTarefa
                .Include(r => r.Usuario)
                .Include(r => r.Tarefa)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ResponsavelTarefa>> ObterResponsaveisPorTarefaIdAsync(int tarefaId)
        {
            return await _context.ResponsaveisTarefa
                .Where(r => r.TarefaId == tarefaId)
                .Include(r => r.Usuario)
                .ToListAsync();
        }

        public async Task<List<ResponsavelTarefa>> ObterTarefasPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.ResponsaveisTarefa
                .Where(r => r.UsuarioId == usuarioId)
                .Include(r => r.Tarefa)
                .ToListAsync();
        }

        public async Task<ResponsavelTarefa> AdicionarResponsavelTarefaAsync(ResponsavelTarefa responsavelTarefa)
        {
            _context.ResponsaveisTarefa.Add(responsavelTarefa);
            await _context.SaveChangesAsync();
            return responsavelTarefa;
        }

        public async Task AdicionarResponsaveisEmLoteAsync(IEnumerable<ResponsavelTarefa> responsaveis)
        {
            await _context.ResponsaveisTarefa.AddRangeAsync(responsaveis);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoverResponsavelTarefaAsync(int id)
        {
            var responsavel = await _context.ResponsaveisTarefa.FindAsync(id);
            if (responsavel == null) return false;

            _context.ResponsaveisTarefa.Remove(responsavel);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoverResponsavelPorTarefaEUsuarioAsync(int tarefaId, int usuarioId)
        {
            var responsavel = await _context.ResponsaveisTarefa
                .FirstOrDefaultAsync(r => r.TarefaId == tarefaId && r.UsuarioId == usuarioId);

            if (responsavel == null) return false;

            _context.ResponsaveisTarefa.Remove(responsavel);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResponsavelTarefaExisteAsync(int id)
        {
            return await _context.ResponsaveisTarefa.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> ResponsavelJaAtribuidoAsync(int tarefaId, int usuarioId)
        {
            return await _context.ResponsaveisTarefa
                .AnyAsync(r => r.TarefaId == tarefaId && r.UsuarioId == usuarioId);
        }
    }
}