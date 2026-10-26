using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class MetaRepository : IMetaRepository
    {
        private readonly CortexDbContext _context;

        public MetaRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<Meta?> ObterMetaPorIdAsync(int id)
        {
            return await _context.Metas
                .Include(m => m.Usuario)
                .Include(m => m.Tarefa)
                .Include(m => m.MetasTarefas)
                    .ThenInclude(mt => mt.Tarefa)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<Meta>> ObterTodasMetasAsync()
        {
            return await _context.Metas
                .Include(m => m.Usuario)
                .Include(m => m.Tarefa)
                .Include(m => m.MetasTarefas)
                    .ThenInclude(mt => mt.Tarefa)
                .ToListAsync();
        }

        public async Task<List<Meta>> ObterMetasPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.Metas
                .Where(m => m.UsuarioId == usuarioId)
                .Include(m => m.Tarefa)
                .Include(m => m.MetasTarefas)
                    .ThenInclude(mt => mt.Tarefa)
                .ToListAsync();
        }

        public async Task<List<Meta>> ObterMetasPorTarefaIdAsync(int tarefaId)
        {
            return await _context.Metas
                .Where(m => m.TarefaId == tarefaId || m.MetasTarefas.Any(mt => mt.TarefaId == tarefaId))
                .Include(m => m.Tarefa)
                .Include(m => m.MetasTarefas)
                    .ThenInclude(mt => mt.Tarefa)
                .ToListAsync();
        }

        public async Task<List<Tarefa>> ObterTarefasDaMetaAsync(int metaId)
        {
            var tarefasAssociadas = await _context.MetasTarefas
                .AsNoTracking()
                .Where(mt => mt.MetaId == metaId)
                .Select(mt => mt.Tarefa)
                .ToListAsync();

            if (tarefasAssociadas.Any())
                return tarefasAssociadas;

            return await _context.Metas
                .AsNoTracking()
                .Where(m => m.Id == metaId)
                .Select(m => m.Tarefa)
                .ToListAsync();
        }

        public async Task<Meta> AdicionarMetaAsync(Meta meta)
        {
            _context.Metas.Add(meta);
            await _context.SaveChangesAsync();
            return meta;
        }

        public async Task<Meta> AtualizarMetaAsync(Meta meta)
        {
            _context.Metas.Update(meta);
            await _context.SaveChangesAsync();
            return meta;
        }

        public async Task<bool> RemoverMetaAsync(int id)
        {
            var meta = await _context.Metas.FindAsync(id);
            if (meta == null) return false;

            _context.Metas.Remove(meta);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MetaExisteAsync(int id)
        {
            return await _context.Metas.AnyAsync(m => m.Id == id);
        }
    }
}