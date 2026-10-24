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
            return await _context.Tarefas
                .Include(t => t.Criador)
                    .ThenInclude(c => c.FotoPerfil)
                .Include(t => t.ResponsaveisTarefas)
                    .ThenInclude(rt => rt.Usuario)
                        .ThenInclude(u => u.FotoPerfil)
                .Include(t => t.Anexos)
                .Include(t => t.Respostas)
                    .ThenInclude(r => r.Usuario)
                .Include(t => t.Metas)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Tarefa>> ObterTodasTarefasAsync()
        {
            return await _context.Tarefas
                .Include(t => t.Criador)
                .Include(t => t.ResponsaveisTarefas)
                    .ThenInclude(rt => rt.Usuario)
                .ToListAsync();
        }

        public async Task<List<Tarefa>> ObterTarefasPorUsuarioIdAsync(int usuarioId)
        {
            return await _context.Tarefas
                .Include(t => t.Criador)
                .Include(t => t.ResponsaveisTarefas)
                    .ThenInclude(rt => rt.Usuario)
                .Where(t => t.CriadorId == usuarioId || t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == usuarioId))
                .ToListAsync();
        }

        public async Task<List<Tarefa>> ObterTarefasComFiltroAsync(
            Status? status = null,
            Prioridade? prioridade = null,
            int? responsavelId = null,
            int? criadorId = null,
            DateTime? dataInicio = null,
            DateTime? dataFim = null,
            string? termoBusca = null)
        {
            var query = _context.Tarefas
                .Include(t => t.Criador)
                .Include(t => t.ResponsaveisTarefas)
                    .ThenInclude(rt => rt.Usuario)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (prioridade.HasValue)
                query = query.Where(t => t.Prioridade == prioridade.Value);

            if (responsavelId.HasValue)
                query = query.Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == responsavelId.Value));

            if (criadorId.HasValue)
                query = query.Where(t => t.CriadorId == criadorId.Value);

            if (dataInicio.HasValue)
                query = query.Where(t => t.DataVencimento >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(t => t.DataVencimento <= dataFim.Value);

            if (!string.IsNullOrWhiteSpace(termoBusca))
            {
                var termo = termoBusca.ToLower();
                query = query.Where(t => t.Titulo.ToLower().Contains(termo) 
                                      || (t.Descricao != null && t.Descricao.ToLower().Contains(termo)));
            }

            return await query.OrderBy(t => t.DataVencimento).ToListAsync();
        }

        public async Task<List<Tarefa>> ObterTarefasVencidasNaoConcluidasAsync(DateTime dataReferencia)
        {
            return await _context.Tarefas
                .Include(t => t.ResponsaveisTarefas)
                    .ThenInclude(rt => rt.Usuario)
                .Where(t => t.Status == Status.Pendente && t.DataVencimento < dataReferencia)
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