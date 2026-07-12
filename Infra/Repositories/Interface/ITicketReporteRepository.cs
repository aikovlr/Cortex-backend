using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface ITicketReporteRepository
    {
        Task<TicketReporte?> ObterTicketReportePorIdAsync(int id);
        
        Task<List<TicketReporte>> ObterTicketReportesPorUsuarioIdAsync(int usuarioId);
        Task<List<TicketReporte>> ObterTicketReportesPorTarefaIdAsync(int tarefaId);
        Task<TicketReporte> AdicionarTicketReporteAsync(TicketReporte ticketReporte);
        Task<TicketReporte> AtualizarTicketReporteAsync(TicketReporte ticketReporte);
        Task<bool> RemoverTicketReporteAsync(int id);
       
        Task<bool> TicketReporteExisteAsync(int id);
    }
}