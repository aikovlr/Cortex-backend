using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class SugestaoService : ISugestaoService
    {
        private readonly ISugestaoRepository _sugestaoRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<SugestaoService> _logger;

        public SugestaoService(
            ISugestaoRepository sugestaoRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            ILogger<SugestaoService> logger)
        {
            _sugestaoRepository = sugestaoRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task<ConsultaSugestaoDTO> CriarSugestaoAsync(CriarSugestaoDTO dto, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            if (tarefa.Status == Status.Concluida)
            {
                throw new InvalidOperationException("Não é possível sugerir alterações em uma tarefa que já está concluída.");
            }

            // RF004 / Plano: apenas responsável pela tarefa pode sugerir
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId);
            if (!ehResponsavel)
            {
                throw new ForbiddenException("Apenas usuários atribuídos como responsáveis pela tarefa podem submeter sugestões de alteração.");
            }

            var sugestao = new Sugestao
            {
                TarefaId = dto.TarefaId,
                UsuarioId = solicitanteId,
                Descricao = dto.Descricao,
                DataCriacao = DateTime.UtcNow
            };

            await _sugestaoRepository.AdicionarSugestaoAsync(sugestao);

            _logger.LogInformation("Nova sugestão criada: SugestaoId {SugestaoId}, TarefaId {TarefaId}, UsuarioId {UsuarioId}",
                sugestao.Id, dto.TarefaId, solicitanteId);

            var sugestaoCompleta = await _sugestaoRepository.ObterSugestaoPorIdAsync(sugestao.Id);
            return MapToConsultaDTO(sugestaoCompleta ?? sugestao);
        }

        public async Task<ConsultaSugestaoDTO> AprovarSugestaoAsync(int sugestaoId, int solicitanteId)
        {
            var sugestao = await _sugestaoRepository.ObterSugestaoPorIdAsync(sugestaoId)
                ?? throw new KeyNotFoundException("Sugestão não encontrada.");

            // RN14: Exige aprovação explícita do criador da tarefa
            if (sugestao.Tarefa.CriadorId != solicitanteId)
            {
                throw new ForbiddenException("Apenas o criador da tarefa possui autorização para aprovar sugestões de alteração.");
            }

            // Impedir re-aprovação ou aprovação de sugestão já rejeitada
            if (sugestao.Status != StatusSugestao.Pendente)
                throw new InvalidOperationException($"Esta sugestão já foi {sugestao.Status.ToString().ToLower()} e não pode ser alterada.");

            // Aplica as alterações na tarefa principal (RN14)
            sugestao.Tarefa.Descricao = string.IsNullOrWhiteSpace(sugestao.Tarefa.Descricao)
                ? $"[Sugestão #{sugestao.Id} Aprovada]: {sugestao.Descricao}"
                : $"{sugestao.Tarefa.Descricao}\n\n[Sugestão #{sugestao.Id} Aprovada]: {sugestao.Descricao}";

            await _tarefaRepository.AtualizarTarefaAsync(sugestao.Tarefa);

            // Persiste o novo status da sugestão
            sugestao.Status = StatusSugestao.Aprovada;
            await _sugestaoRepository.AtualizarSugestaoAsync(sugestao);

            _logger.LogInformation("Auditoria: Sugestão processada. SugestaoId: {SugestaoId}, TarefaId: {TarefaId}, Acao: {Acao}, SolicitanteId: {SolicitanteId}",
                sugestao.Id, sugestao.TarefaId, "Aprovada", solicitanteId);

            return MapToConsultaDTO(sugestao);
        }

        public async Task<ConsultaSugestaoDTO> RejeitarSugestaoAsync(int sugestaoId, int solicitanteId)
        {
            var sugestao = await _sugestaoRepository.ObterSugestaoPorIdAsync(sugestaoId)
                ?? throw new KeyNotFoundException("Sugestão não encontrada.");

            // RN14: Apenas o criador da tarefa pode rejeitar
            if (sugestao.Tarefa.CriadorId != solicitanteId)
            {
                throw new ForbiddenException("Apenas o criador da tarefa possui autorização para rejeitar sugestões de alteração.");
            }

            if (sugestao.Status != StatusSugestao.Pendente)
                throw new InvalidOperationException($"Esta sugestão já foi {sugestao.Status.ToString().ToLower()} e não pode ser alterada.");

            // Persiste o status de rejeição
            sugestao.Status = StatusSugestao.Rejeitada;
            await _sugestaoRepository.AtualizarSugestaoAsync(sugestao);

            _logger.LogInformation("Auditoria: Sugestão processada. SugestaoId: {SugestaoId}, TarefaId: {TarefaId}, Acao: {Acao}, SolicitanteId: {SolicitanteId}",
                sugestao.Id, sugestao.TarefaId, "Rejeitada", solicitanteId);

            return MapToConsultaDTO(sugestao);
        }

        public async Task<IEnumerable<ConsultaSugestaoDTO>> ListarSugestoesPorTarefaAsync(int tarefaId, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId);
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = usuario != null && usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            if (!ehCriador && !ehResponsavel && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para visualizar as sugestões desta tarefa.");
            }

            var sugestoes = await _sugestaoRepository.ObterSugestoesPorTarefaIdAsync(tarefaId);
            return sugestoes.Select(MapToConsultaDTO);
        }

        private static ConsultaSugestaoDTO MapToConsultaDTO(Sugestao sugestao)
        {
            return new ConsultaSugestaoDTO
            {
                Id = sugestao.Id,
                Descricao = sugestao.Descricao,
                DataCriacao = sugestao.DataCriacao,
                Status = sugestao.Status,
                TarefaId = sugestao.TarefaId,
                UsuarioId = sugestao.UsuarioId,
                Usuario = sugestao.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = sugestao.Usuario.Id,
                    Nome = sugestao.Usuario.Nome,
                    Email = sugestao.Usuario.Email,
                    Telefone = sugestao.Usuario.Telefone,
                    FotoPerfil = sugestao.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = sugestao.Usuario.FotoPerfil.Id,
                        UrlCaminho = sugestao.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = sugestao.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = sugestao.Usuario.FotoPerfil.MimeType,
                        DataEnvio = sugestao.Usuario.FotoPerfil.DataEnvio
                    } : null
                } : null,
                Tarefa = sugestao.Tarefa != null ? new ConsultaTarefaDTO
                {
                    Id = sugestao.Tarefa.Id,
                    Titulo = sugestao.Tarefa.Titulo,
                    Descricao = sugestao.Tarefa.Descricao,
                    DataVencimento = sugestao.Tarefa.DataVencimento,
                    Pontuacao = sugestao.Tarefa.Pontuacao,
                    Status = sugestao.Tarefa.Status,
                    Prioridade = sugestao.Tarefa.Prioridade,
                    CriadorId = sugestao.Tarefa.CriadorId
                } : null
            };
        }
    }
}
