using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class MetaService : IMetaService
    {
        private readonly IMetaRepository _metaRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEquipeAuthorizationService _authService;
        private readonly ILogger<MetaService> _logger;

        public MetaService(
            IMetaRepository metaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeAuthorizationService authService,
            ILogger<MetaService> logger)
        {
            _metaRepository = metaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<ConsultaMetaDTO> CriarMetaAsync(CriarMetaDTO dto, int solicitanteId)
        {
            // RN07: Toda meta deve estar vinculada a no mínimo uma tarefa válida
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
            if (tarefa == null)
            {
                throw new ArgumentException("A meta deve estar vinculada a uma tarefa válida e existente.");
            }

            DateTime? dataFechamentoUtc = dto.DataFechamento?.ToUniversalTime();
            if (dataFechamentoUtc.HasValue)
            {

                // RN15: Metas criadas devem ter uma data de término de no máximo 12 meses
                var limiteMaximo = DateTime.UtcNow.AddMonths(12);
                if (dataFechamentoUtc > limiteMaximo)
                {
                    throw new ArgumentException("A data de fechamento da meta não pode exceder 12 meses a partir da data atual.");
                }
            }

            // RN08: Verifica se a tarefa vinculada já está concluída
            var concluida = tarefa.Status == Status.Concluida;

            var meta = new Meta
            {
                Nome = dto.Nome,
                DataFechamento = dataFechamentoUtc,
                Concluida = concluida,
                TarefaId = dto.TarefaId,
                UsuarioId = solicitanteId,
                Tarefa = tarefa
            };

            await _metaRepository.AdicionarMetaAsync(meta);

            _logger.LogInformation("Meta criada: MetaId {MetaId}, Nome '{Nome}', UsuarioId {UsuarioId}, TarefaId {TarefaId}",
                meta.Id, meta.Nome, solicitanteId, dto.TarefaId);

            var metaCompleta = await _metaRepository.ObterMetaPorIdAsync(meta.Id);
            return MapToConsultaDTO(metaCompleta ?? meta);
        }

        public async Task<ConsultaMetaDTO?> ObterMetaPorIdAsync(int id, int solicitanteId)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(id);
            if (meta == null)
                return null;

            // RN09: Usuários comuns só podem visualizar suas próprias metas
            var tarefa = meta.Tarefa ?? await _tarefaRepository.ObterTarefaPorIdAsync(meta.TarefaId);
            var ehAdminOuDono = tarefa != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (meta.UsuarioId != solicitanteId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para visualizar esta meta.");
            }

            return MapToConsultaDTO(meta);
        }

        public async Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorUsuarioAsync(int usuarioId, int solicitanteId)
        {
            // RN09: Usuários comuns (Membro) só podem listar suas próprias metas
            var ehAdminOuDono = await _authService.EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(solicitanteId, usuarioId);

            if (solicitanteId != usuarioId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você só possui permissão para visualizar suas próprias metas.");
            }

            var metas = await _metaRepository.ObterMetasPorUsuarioIdAsync(usuarioId);
            return metas.Select(MapToConsultaDTO);
        }

        public async Task<ConsultaMetaDTO> AtualizarMetaAsync(int id, AtualizarMetaDTO dto, int solicitanteId)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(id);
            if (meta == null)
                throw new KeyNotFoundException("Meta não encontrada.");

            var tarefa = meta.Tarefa ?? await _tarefaRepository.ObterTarefaPorIdAsync(meta.TarefaId);
            var ehAdminOuDono = tarefa != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (meta.UsuarioId != solicitanteId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para alterar esta meta.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Nome))
                meta.Nome = dto.Nome;

            if (dto.TarefaId > 0 && dto.TarefaId != meta.TarefaId)
            {
                // RN07: Validar se a nova tarefa existe
                var novaTarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
                if (novaTarefa == null)
                    throw new ArgumentException("A nova tarefa vinculada não foi encontrada.");

                meta.TarefaId = dto.TarefaId;
                meta.Tarefa = novaTarefa;

                // RN08: Checar conclusão
                if (novaTarefa.Status == Status.Concluida)
                    meta.Concluida = true;
            }

            if (dto.DataFechamento.HasValue)
            {
                var dataUtc = dto.DataFechamento.Value.ToUniversalTime();

                // RN15: Validade máxima de 12 meses
                var limiteMaximo = DateTime.UtcNow.AddMonths(12);
                if (dataUtc > limiteMaximo)
                {
                    throw new ArgumentException("A data de fechamento da meta não pode exceder 12 meses a partir da data atual.");
                }

                meta.DataFechamento = dataUtc;
            }

            if (dto.Concluida.HasValue)
            {
                meta.Concluida = dto.Concluida.Value;
            }

            await _metaRepository.AtualizarMetaAsync(meta);

            _logger.LogInformation("Meta atualizada: MetaId {MetaId} por SolicitanteId {SolicitanteId}",
                id, solicitanteId);

            var metaAtualizada = await _metaRepository.ObterMetaPorIdAsync(id);
            return MapToConsultaDTO(metaAtualizada ?? meta);
        }

        public async Task<bool> DeletarMetaAsync(int id, int solicitanteId)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(id);
            if (meta == null)
                return false;

            var tarefa = meta.Tarefa ?? await _tarefaRepository.ObterTarefaPorIdAsync(meta.TarefaId);
            var ehAdminOuDono = tarefa != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (meta.UsuarioId != solicitanteId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para excluir esta meta.");
            }

            var removida = await _metaRepository.RemoverMetaAsync(id);

            if (removida)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "Meta", id, solicitanteId);
            }

            return removida;
        }

        public async Task<int> VerificarConclusaoMetaAsync(int tarefaId)
        {
            // RN08: só marca a meta como Concluída se TODAS as tarefas vinculadas estiverem concluídas
            var metas = await _metaRepository.ObterMetasPorTarefaIdAsync(tarefaId);
            var metasConcluidas = 0;

            foreach (var meta in metas)
            {
                // Busca TODAS as tarefas vinculadas à esta meta
                var tarefasDaMeta = await _metaRepository.ObterTarefasDaMetaAsync(meta.Id);

                // Só conclui se TODAS estiverem concluídas
                var todasConcluidas = tarefasDaMeta.Count > 0 && tarefasDaMeta.All(t => t.Status == Status.Concluida);

                if (todasConcluidas && meta.Concluida != true)
                {
                    meta.Concluida = true;
                    await _metaRepository.AtualizarMetaAsync(meta);
                    metasConcluidas++;

                    _logger.LogInformation(
                        "Meta {MetaId} ('{Nome}') concluída automaticamente — todas as tarefas vinculadas estão concluídas.",
                        meta.Id, meta.Nome);
                }
            }

            return metasConcluidas;
        }

        private static ConsultaMetaDTO MapToConsultaDTO(Meta meta)
        {
            return new ConsultaMetaDTO
            {
                Id = meta.Id,
                Nome = meta.Nome,
                DataFechamento = meta.DataFechamento,
                Concluida = meta.Concluida ?? false,
                TarefaId = meta.TarefaId,
                UsuarioId = meta.UsuarioId,
                Usuario = meta.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = meta.Usuario.Id,
                    Nome = meta.Usuario.Nome,
                    Email = meta.Usuario.Email,
                    Telefone = meta.Usuario.Telefone,
                    FotoPerfil = meta.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = meta.Usuario.FotoPerfil.Id,
                        UrlCaminho = meta.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = meta.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = meta.Usuario.FotoPerfil.MimeType,
                        DataEnvio = meta.Usuario.FotoPerfil.DataEnvio
                    } : null
                } : null,
                Tarefa = meta.Tarefa != null ? new ConsultaTarefaDTO
                {
                    Id = meta.Tarefa.Id,
                    Titulo = meta.Tarefa.Titulo,
                    Descricao = meta.Tarefa.Descricao,
                    DataVencimento = meta.Tarefa.DataVencimento,
                    Pontuacao = meta.Tarefa.Pontuacao,
                    Status = meta.Tarefa.Status,
                    Prioridade = meta.Tarefa.Prioridade,
                    CriadorId = meta.Tarefa.CriadorId
                } : null
            };
        }
    }
}
