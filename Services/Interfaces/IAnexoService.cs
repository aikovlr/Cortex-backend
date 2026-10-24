using Cortex.DTOs.Response;
using Cortex.Entities;

namespace Cortex.Services.Interfaces
{
    public interface IAnexoService
    {
        Task<ConsultaAnexoDTO> AdicionarAnexoTarefaAsync(int tarefaId, IFormFile arquivo, int solicitanteId);
        Task<IEnumerable<ConsultaAnexoDTO>> AdicionarAnexosTarefaAsync(int tarefaId, List<IFormFile> arquivos, int solicitanteId);
        Task<ConsultaAnexoDTO?> ObterAnexoPorIdAsync(int id);
        Task<IEnumerable<ConsultaAnexoDTO>> ListarAnexosPorTarefaAsync(int tarefaId, int solicitanteId);
        Task<IEnumerable<ConsultaAnexoDTO>> ListarAnexosPorEntidadeAsync(TipoEntidade tipoEntidade, int entidadeId, int solicitanteId);
        Task<bool> DeletarAnexoAsync(int id, int solicitanteId);
    }
}
