using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class SugestaoRepository : ISugestaoRepository
    {
        private readonly CortexDbContext _context;

        public SugestaoRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<Sugestao?> ObterSugestaoPorIdAsync(int id)
        {
            return await _context.Sugestoes
                .Include(s => s.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .Include(s => s.Tarefa)
                    .ThenInclude(t => t.Criador)
                .Include(s => s.Tarefa)
                    .ThenInclude(t => t.ResponsaveisTarefas)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Sugestao>> ObterSugestoesPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.Sugestoes
                .Where(s => s.UsuarioId == usuarioId)
                .Include(s => s.Tarefa)
                .Include(s => s.Usuario)
                .OrderByDescending(s => s.DataCriacao)
                .ToListAsync();
        }

        public async Task<List<Sugestao>> ObterSugestoesPorTarefaIdAsync(int tarefaId)
        {
            return await _context.Sugestoes
                .Where(s => s.TarefaId == tarefaId)
                .Include(s => s.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .OrderByDescending(s => s.DataCriacao)
                .ToListAsync();
        }

        public async Task<Sugestao> AdicionarSugestaoAsync(Sugestao sugestao)
        {
            _context.Sugestoes.Add(sugestao);
            await _context.SaveChangesAsync();
            return sugestao;
        }

        public async Task<Sugestao> AtualizarSugestaoAsync(Sugestao sugestao)
        {
            _context.Sugestoes.Update(sugestao);
            await _context.SaveChangesAsync();
            return sugestao;
        }

        public async Task<bool> RemoverSugestaoAsync(int id)
        {
            var sugestao = await _context.Sugestoes.FindAsync(id);
            if (sugestao == null) return false;

            _context.Sugestoes.Remove(sugestao);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SugestaoExisteAsync(int id)
        {
            return await _context.Sugestoes.AnyAsync(s => s.Id == id);
        }
    }
}