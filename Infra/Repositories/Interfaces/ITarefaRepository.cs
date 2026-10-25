using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface ITarefaRepository
    {
        Task<Tarefa?> ObterTarefaPorIdAsync(int id);
        Task<List<Tarefa>> ObterTodasTarefasAsync();
        Task<List<Tarefa>> ObterTarefasPorUsuarioIdAsync(int usuarioId);
        Task<List<Tarefa>> ObterTarefasComFiltroAsync(
            Status? status = null,
            Prioridade? prioridade = null,
            int? responsavelId = null,
            int? criadorId = null,
            DateTime? dataInicio = null,
            DateTime? dataFim = null,
            string? termoBusca = null);
        Task<List<Tarefa>> ObterTarefasVencidasNaoConcluidasAsync(DateTime dataReferencia);
        Task<List<Tarefa>> ObterTarefasPendentesAVencerAsync(DateTime dataReferencia);
        Task<Tarefa> AdicionarTarefaAsync(Tarefa tarefa);
        Task<Tarefa> AtualizarTarefaAsync(Tarefa tarefa);
        Task<bool> RemoverTarefaAsync(int id);
        Task<bool> TarefaExisteAsync(int id);
    }
}