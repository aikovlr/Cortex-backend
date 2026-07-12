using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class TarefaRepository : ITarefaRepository
    {
        private readonly CortexDbContext _context;

        public TarefaRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<Tarefa?> ObterTarefaPorIdAsync(int id)
        {
            return await _context.Tarefas.FindAsync(id);
        }

        public async Task<List<Tarefa>> ObterTodasTarefasAsync()
        {
            return await _context.Tarefas.ToListAsync();
        }

        public async Task<List<Tarefa>> ObterTarefasPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.ResponsaveisTarefa
                .Where(rt => rt.UsuarioId == usuarioId)
                .Select(rt => rt.Tarefa)
                .ToListAsync();
        }

        public async Task<Tarefa> AdicionarTarefaAsync(Tarefa tarefa)
        {
            _context.Tarefas.Add(tarefa);
            await _context.SaveChangesAsync();
            return tarefa;
        }

        public async Task<Tarefa> AtualizarTarefaAsync(Tarefa tarefa)
        {
            _context.Tarefas.Update(tarefa);
            await _context.SaveChangesAsync();
            return tarefa;
        }

        public async Task<bool> RemoverTarefaAsync(int id)
        {
            var tarefa = await _context.Tarefas.FindAsync(id);
            if (tarefa == null) return false;

            _context.Tarefas.Remove(tarefa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TarefaExisteAsync(int id)
        {
            return await _context.Tarefas.AnyAsync(t => t.Id == id);
        }
    }
}