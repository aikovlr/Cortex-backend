using Cortex.DTOs.Request;
using Cortex.DTOs.Response;

namespace Cortex.Services.Interfaces
{
    public interface ITicketReporteService
    {
        Task<ConsultaTicketReporteDTO> CriarTicketAsync(CriarTicketReporteDTO dto, int solicitanteId);
        Task<ConsultaTicketReporteDTO> FecharTicketAsync(int ticketId, int solicitanteId);
        Task<IEnumerable<ConsultaTicketReporteDTO>> ListarTicketsPorTarefaAsync(int tarefaId, int solicitanteId);
        Task<IEnumerable<ConsultaTicketReporteDTO>> ListarTicketsAbertosAsync(int equipeId, int solicitanteId);
    }
}
