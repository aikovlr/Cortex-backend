using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class AnexoRepository : IAnexoRepository
    {
        private readonly CortexDbContext _context;

        public AnexoRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<Anexo?> ObterAnexoPorIdAsync(int id)
        {
            return await _context.Anexos.FindAsync(id);
        }

        public async Task<List<Anexo>> ObterAnexosPorTarefaIdAsync(int tarefaId)
        {
            return await _context.Anexos.Where(a => a.TarefaId == tarefaId).ToListAsync();
        }

        public async Task<List<Anexo>> ObterAnexosPorRespostaIdAsync(int respostaId)
        {
            return await _context.Anexos.Where(a => a.RespostaTarefaId == respostaId).ToListAsync();
        }

        public async Task<List<Anexo>> ObterAnexosPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.Anexos.Where(a => a.UsuarioId == usuarioId).ToListAsync();
        }

        public async Task<Anexo> AdicionarAnexoAsync(Anexo anexo)
        {
            _context.Anexos.Add(anexo);
            await _context.SaveChangesAsync();
            return anexo;
        }

        public async Task<Anexo> AtualizarAnexoAsync(Anexo anexo)
        {
            _context.Entry(anexo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return anexo;
        }

        public async Task<bool> RemoverAnexoAsync(int id)
        {
            var anexo = await _context.Anexos.FindAsync(id);
            if (anexo == null) return false;

            _context.Anexos.Remove(anexo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AnexoExisteAsync(int id)
        {
            return await _context.Anexos.AnyAsync(e => e.Id == id);
        }
    }
}