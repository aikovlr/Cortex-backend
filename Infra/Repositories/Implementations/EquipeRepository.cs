using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class EquipeRepository : IEquipeRepository
    {
        private readonly CortexDbContext _context;

        public EquipeRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<List<Equipe>> ObterTodasEquipesAsync()
        {
            return await _context.Equipes.ToListAsync();
        }
        
        public async Task<Equipe?> ObterEquipePorIdAsync(int id)
        {
            return await _context.Equipes.FindAsync(id);
        }

        public async Task<List<Equipe>> ObterEquipesPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.MembrosEquipe
                .Where(me => me.UsuarioId == usuarioId)
                .Select(me => me.Equipe)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Equipe> AdicionarEquipeAsync(Equipe equipe)
        {
            _context.Equipes.Add(equipe);
            await _context.SaveChangesAsync();
            return equipe;
        }

        public async Task<Equipe> AtualizarEquipeAsync(Equipe equipe)
        {
            _context.Equipes.Update(equipe);
            await _context.SaveChangesAsync();
            return equipe;
        }

        public async Task<bool> RemoverEquipeAsync(int id)
        {
            var equipe = await _context.Equipes.FindAsync(id);
            if (equipe == null) return false;

            _context.Equipes.Remove(equipe);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EquipeExisteAsync(int id)
        {
            return await _context.Equipes.AnyAsync(e => e.Id == id);
        }
    }
}