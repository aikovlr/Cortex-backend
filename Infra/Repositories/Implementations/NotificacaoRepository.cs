using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories.Implementations
{
    public class NotificacaoRepository : INotificacaoRepository
    {
        private readonly CortexDbContext _context;

        public NotificacaoRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<Notificacao> AdicionarAsync(Notificacao notificacao)
        {
            _context.Notificacoes.Add(notificacao);
            await _context.SaveChangesAsync();
            return notificacao;
        }

        public async Task<IEnumerable<Notificacao>> ObterPorUsuarioAsync(int usuarioId)
        {
            return await _context.Notificacoes
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.DataCriacao)
                .ToListAsync();
        }

        public async Task<Notificacao?> ObterPorIdAsync(int id)
        {
            return await _context.Notificacoes.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task AtualizarAsync(Notificacao notificacao)
        {
            _context.Notificacoes.Update(notificacao);
            await _context.SaveChangesAsync();
        }

        public async Task MarcarTodasComoLidasAsync(int usuarioId)
        {
            var naoLidas = await _context.Notificacoes
                .Where(n => n.UsuarioId == usuarioId && !n.Lida)
                .ToListAsync();

            if (naoLidas.Count == 0) return;

            foreach (var item in naoLidas)
            {
                item.Lida = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExisteNotificacaoAsync(int usuarioId, string tipo, int tarefaId)
        {
            return await _context.Notificacoes.AnyAsync(n => n.UsuarioId == usuarioId && n.Tipo == tipo && n.TarefaId == tarefaId);
        }

        public async Task LimparNotificacoesAsync(int usuarioId)
        {
            var notificacoes = await _context.Notificacoes
                .Where(n => n.UsuarioId == usuarioId)
                .ToListAsync();

            if (notificacoes.Count == 0) return;

            _context.Notificacoes.RemoveRange(notificacoes);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoverNotificacaoAsync(int id, int usuarioId)
        {
            var notif = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId);

            if (notif == null) return false;

            _context.Notificacoes.Remove(notif);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
