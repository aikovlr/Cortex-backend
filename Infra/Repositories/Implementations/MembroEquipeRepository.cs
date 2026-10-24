using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class MembroEquipeRepository : IMembroEquipeRepository
    {
        private readonly CortexDbContext _context;

        public MembroEquipeRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<MembroEquipe?> ObterMembroEquipePorIdAsync(int id)
        {
            return await _context.MembrosEquipe
                .Include(me => me.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .Include(me => me.Equipe)
                .FirstOrDefaultAsync(me => me.Id == id);
        }

        public async Task<MembroEquipe?> ObterMembroPorEquipeEUsuarioAsync(int equipeId, int usuarioId)
        {
            return await _context.MembrosEquipe
                .Include(me => me.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .Include(me => me.Equipe)
                .FirstOrDefaultAsync(me => me.EquipeId == equipeId && me.UsuarioId == usuarioId);
        }

        public async Task<List<MembroEquipe>> ObterMembrosEquipePorEquipeIdAsync(int equipeId)
        {
            return await _context.MembrosEquipe
                .Where(me => me.EquipeId == equipeId)
                .Include(me => me.Usuario)
                    .ThenInclude(u => u.FotoPerfil)
                .ToListAsync();
        }

        public async Task<List<MembroEquipe>> ObterMembrosPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.MembrosEquipe
                .Where(me => me.UsuarioId == usuarioId)
                .Include(me => me.Equipe)
                .ToListAsync();
        }

        public async Task<MembroEquipe> AdicionarMembroEquipeAsync(MembroEquipe membroEquipe)
        {
            _context.MembrosEquipe.Add(membroEquipe);
            await _context.SaveChangesAsync();
            return membroEquipe;
        }

        public async Task<MembroEquipe> AtualizarMembroEquipeAsync(MembroEquipe membroEquipe)
        {
            _context.MembrosEquipe.Update(membroEquipe);
            await _context.SaveChangesAsync();
            return membroEquipe;
        }

        public async Task<bool> RemoverMembroEquipeAsync(int id)
        {
            var membroEquipe = await _context.MembrosEquipe.FindAsync(id);
            if (membroEquipe == null) return false;

            _context.MembrosEquipe.Remove(membroEquipe);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MembroEquipeExisteAsync(int id)
        {
            return await _context.MembrosEquipe.AnyAsync(me => me.Id == id);
        }

        public async Task<bool> MembroExisteNaEquipeAsync(int equipeId, int usuarioId)
        {
            return await _context.MembrosEquipe
                .AnyAsync(me => me.EquipeId == equipeId && me.UsuarioId == usuarioId);
        }
    }
}