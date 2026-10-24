using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class TicketReporteService : ITicketReporteService
    {
        private readonly ITicketReporteRepository _ticketReporteRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEquipeRepository _equipeRepository;
        private readonly IMembroEquipeRepository _membroEquipeRepository;
        private readonly ILogger<TicketReporteService> _logger;

        public TicketReporteService(
            ITicketReporteRepository ticketReporteRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeRepository equipeRepository,
            IMembroEquipeRepository membroEquipeRepository,
            ILogger<TicketReporteService> logger)
        {
            _ticketReporteRepository = ticketReporteRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _equipeRepository = equipeRepository;
            _membroEquipeRepository = membroEquipeRepository;
            _logger = logger;
        }

        public async Task<ConsultaTicketReporteDTO> CriarTicketAsync(CriarTicketReporteDTO dto, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            if (tarefa.Status == Status.Concluida)
            {
                throw new InvalidOperationException("Não é possível abrir ticket de reporte para uma tarefa já concluída.");
            }

            // RF008: apenas responsáveis pela tarefa podem abrir ticket de impedimento
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId);
            if (!ehResponsavel)
            {
                throw new ForbiddenException("Apenas usuários atribuídos como responsáveis pela tarefa possuem permissão para abrir tickets de reporte.");
            }

            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            var ticket = new TicketReporte
            {
                TarefaId = dto.TarefaId,
                UsuarioId = solicitanteId,
                Descricao = dto.Descricao,
                Ativo = true,
                DataCriacao = DateTime.UtcNow,
                Tarefa = tarefa,
                Usuario = usuario
            };

            await _ticketReporteRepository.AdicionarTicketReporteAsync(ticket);

            _logger.LogInformation("Auditoria: Abertura de ticket. TicketId: {TicketId}, TarefaId: {TarefaId}, UsuarioId: {UsuarioId}",
                ticket.Id, dto.TarefaId, solicitanteId);

            var ticketCompleto = await _ticketReporteRepository.ObterTicketReportePorIdAsync(ticket.Id);
            return MapToConsultaDTO(ticketCompleto ?? ticket);
        }

        public async Task<ConsultaTicketReporteDTO> FecharTicketAsync(int ticketId, int solicitanteId)
        {
            var ticket = await _ticketReporteRepository.ObterTicketReportePorIdAsync(ticketId);
            if (ticket == null)
                throw new KeyNotFoundException("Ticket de reporte não encontrado.");

            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = usuario != null && usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);
            var ehCriadorTarefa = ticket.Tarefa != null && ticket.Tarefa.CriadorId == solicitanteId;

            // Dono/Admin ou criador da tarefa fecha o ticket
            if (!ehAdminOuDono && !ehCriadorTarefa)
            {
                throw new ForbiddenException("Apenas administradores, donos ou o criador da tarefa possuem autorização para fechar tickets de reporte.");
            }

            ticket.Ativo = false;
            await _ticketReporteRepository.AtualizarTicketReporteAsync(ticket);

            _logger.LogInformation("Ticket de reporte {TicketId} na TarefaId {TarefaId} fechado por SolicitanteId {SolicitanteId}",
                ticket.Id, ticket.TarefaId, solicitanteId);

            return MapToConsultaDTO(ticket);
        }

        public async Task<IEnumerable<ConsultaTicketReporteDTO>> ListarTicketsPorTarefaAsync(int tarefaId, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = usuario != null && usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);
            var ehCriador = tarefa.CriadorId == solicitanteId;

            var tickets = await _ticketReporteRepository.ObterTicketReportesPorTarefaIdAsync(tarefaId);

            // RN09: Usuários comuns (Membro) só podem visualizar seus próprios tickets
            if (!ehAdminOuDono && !ehCriador)
            {
                tickets = tickets.Where(t => t.UsuarioId == solicitanteId).ToList();
            }

            return tickets.Select(MapToConsultaDTO);
        }

        public async Task<IEnumerable<ConsultaTicketReporteDTO>> ListarTicketsAbertosAsync(int equipeId, int solicitanteId)
        {
            var equipeExiste = await _equipeRepository.EquipeExisteAsync(equipeId);
            if (!equipeExiste)
                throw new KeyNotFoundException("Equipe não encontrada.");

            // Apenas Admin ou Dono da respectiva equipe
            var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId);
            if (membro == null || (membro.Cargo != Cargo.Dono && membro.Cargo != Cargo.Admin))
            {
                throw new ForbiddenException("Apenas o Dono ou Administrador da equipe podem listar os tickets abertos.");
            }

            var ticketsAbertos = await _ticketReporteRepository.ObterTicketsAbertosPorEquipeIdAsync(equipeId);
            return ticketsAbertos.Select(MapToConsultaDTO);
        }

        private static ConsultaTicketReporteDTO MapToConsultaDTO(TicketReporte ticket)
        {
            return new ConsultaTicketReporteDTO
            {
                Id = ticket.Id,
                Descricao = ticket.Descricao,
                Ativo = ticket.Ativo,
                DataCriacao = ticket.DataCriacao,
                TarefaId = ticket.TarefaId,
                UsuarioId = ticket.UsuarioId,
                Usuario = ticket.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = ticket.Usuario.Id,
                    Nome = ticket.Usuario.Nome,
                    Email = ticket.Usuario.Email,
                    Telefone = ticket.Usuario.Telefone,
                    FotoPerfil = ticket.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = ticket.Usuario.FotoPerfil.Id,
                        UrlCaminho = ticket.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = ticket.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = ticket.Usuario.FotoPerfil.MimeType,
                        DataEnvio = ticket.Usuario.FotoPerfil.DataEnvio
                    } : null
                } : null,
                Tarefa = ticket.Tarefa != null ? new ConsultaTarefaDTO
                {
                    Id = ticket.Tarefa.Id,
                    Titulo = ticket.Tarefa.Titulo,
                    Descricao = ticket.Tarefa.Descricao,
                    DataVencimento = ticket.Tarefa.DataVencimento,
                    Pontuacao = ticket.Tarefa.Pontuacao,
                    Status = ticket.Tarefa.Status,
                    Prioridade = ticket.Tarefa.Prioridade,
                    CriadorId = ticket.Tarefa.CriadorId
                } : null
            };
        }
    }
}
