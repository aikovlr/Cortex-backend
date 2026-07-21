using Microsoft.EntityFrameworkCore;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;

namespace Cortex.Repositories.Implementations
{
    public class ResponsavelTarefaRepository : IUsuarioRepository
    {
        private readonly CortexDbContext _context;

        public ResponsavelTarefaRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObterUsuarioPorIdAsync(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        public async Task<Usuario?> ObterUsuarioPorEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<Usuario>> ObterTodosUsuariosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<List<Usuario>> ListarUsuariosAsync()
        {
            return await ObterTodosUsuariosAsync();
        }

        public async Task<Usuario> AdicionarUsuarioAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<Usuario> AtualizarUsuarioAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<bool> RemoverUsuarioAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UsuarioExisteAsync(int id)
        {
            return await _context.Usuarios.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> UsuarioExistePorEmailAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> UsuarioTemTarefasEmAndamentoAsync(int usuarioId)
        {
            return await _context.Tarefas.AnyAsync(t => t.CriadorId == usuarioId && t.Status == Status.Pendente);
        }

        public async Task<bool> UsuarioEhResponsavelPorTarefaEmAndamentoAsync(int usuarioId)
        {
            return await _context.ResponsaveisTarefa.AnyAsync(r => r.UsuarioId == usuarioId
                && r.Tarefa.Status == Status.Pendente);
        }
    }
}