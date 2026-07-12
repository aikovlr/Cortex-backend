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
            return await _context.Metas.FindAsync(id);
        }

        public async Task<List<Meta>> ObterTodasMetasAsync()
        {
            return await _context.Metas.ToListAsync();
        }

        public async Task<List<Meta>> ObterMetasPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.Metas
                .Where(m => m.UsuarioId == usuarioId)
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