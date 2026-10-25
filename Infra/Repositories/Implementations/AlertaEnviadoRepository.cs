using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories.Implementations
{
    public class AlertaEnviadoRepository : IAlertaEnviadoRepository
    {
        private readonly CortexDbContext _context;

        public AlertaEnviadoRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteAlertaEnviadoAsync(int usuarioId, int tarefaId, CanalNotificacao canal, DateTime desdeUtc)
        {
            return await _context.Set<AlertaEnviado>()
                .AnyAsync(a => a.UsuarioId == usuarioId 
                            && a.TarefaId == tarefaId 
                            && a.Canal == canal 
                            && a.EnviadoEm >= desdeUtc);
        }

        public async Task RegistrarAsync(int usuarioId, int tarefaId, CanalNotificacao canal)
        {
            var alerta = new AlertaEnviado
            {
                UsuarioId = usuarioId,
                TarefaId = tarefaId,
                Canal = canal,
                EnviadoEm = DateTime.UtcNow
            };

            await _context.Set<AlertaEnviado>().AddAsync(alerta);
            await _context.SaveChangesAsync();
        }
    }
}
