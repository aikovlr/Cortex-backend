using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class RespostaTarefaRepository : IRespostaTarefaRepository
    {
        private readonly CortexDbContext _context;

        public RespostaTarefaRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<RespostaTarefa?> ObterRespostaTarefaPorIdAsync(int id)
        {
            return await _context.RespostasTarefa.FindAsync(id);
        }

        public async Task<List<RespostaTarefa>> ObterRespostasTarefaPorTarefaIdAsync(int tarefaId)
        {
            return await _context.RespostasTarefa
                .Where(rt => rt.TarefaId == tarefaId)
                .ToListAsync();
        }

        public async Task<List<RespostaTarefa>> ObterRespostasTarefaPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.RespostasTarefa
                .Where(rt => rt.UsuarioId == usuarioId)
                .ToListAsync();
        }

        public async Task<RespostaTarefa> AdicionarRespostaTarefaAsync(RespostaTarefa respostaTarefa)
        {
            _context.RespostasTarefa.Add(respostaTarefa);
            await _context.SaveChangesAsync();
            return respostaTarefa;
        }

        public async Task<RespostaTarefa> AtualizarRespostaTarefaAsync(RespostaTarefa respostaTarefa)
        {
            _context.RespostasTarefa.Update(respostaTarefa);
            await _context.SaveChangesAsync();
            return respostaTarefa;
        }

        public async Task<bool> RemoverRespostaTarefaAsync(int id)
        {
            var respostaTarefa = await _context.RespostasTarefa.FindAsync(id);
            if (respostaTarefa == null) return false;

            _context.RespostasTarefa.Remove(respostaTarefa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RespostaTarefaExisteAsync(int id)
        {
            return await _context.RespostasTarefa.AnyAsync(rt => rt.Id == id);
        }
    }
}