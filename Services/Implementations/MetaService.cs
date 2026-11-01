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
        private readonly INotificacaoService? _notificacaoService;
        private readonly ILogger<MetaService> _logger;

        public MetaService(
            IMetaRepository metaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeAuthorizationService authService,
            ILogger<MetaService> logger,
            INotificacaoService? notificacaoService = null)
        {
            _metaRepository = metaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _authService = authService;
            _logger = logger;
            _notificacaoService = notificacaoService;
        }

        public async Task<ConsultaMetaDTO> CriarMetaAsync(CriarMetaDTO dto, int solicitanteId)
        {
            var tarefaIds = (dto.TarefaIds != null && dto.TarefaIds.Any())
                ? dto.TarefaIds.Distinct().Where(id => id > 0).ToList()
                : (dto.TarefaId > 0 ? new List<int> { dto.TarefaId } : new List<int>());

            if (!tarefaIds.Any())
            {
                throw new ArgumentException("A meta deve estar vinculada a no mínimo uma tarefa válida.");
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

            var tarefas = new List<Tarefa>();
            foreach (var tId in tarefaIds)
            {
                var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tId);
                if (tarefa == null)
                {
                    throw new ArgumentException($"A tarefa #{tId} vinculada não foi encontrada ou não é uma tarefa válida.");
                }
                tarefas.Add(tarefa);
            }

            // RN08: Concluida se TODAS as tarefas vinculadas estiverem Concluidas
            var todasConcluidas = tarefas.All(t => t.Status == Status.Concluida);

            var meta = new Meta
            {
                Nome = dto.Nome,
                DataFechamento = dataFechamentoUtc,
                Concluida = todasConcluidas,
                TarefaId = tarefaIds.First(),
                UsuarioId = solicitanteId,
                Tarefa = tarefas.First()
            };

            foreach (var tId in tarefaIds)
            {
                meta.MetasTarefas.Add(new MetaTarefa
                {
                    TarefaId = tId
                });
            }

            await _metaRepository.AdicionarMetaAsync(meta);

            if (meta.Concluida == true && _notificacaoService != null && solicitanteId > 0)
            {
                try
                {
                    await _notificacaoService.CriarNotificacaoAsync(
                        solicitanteId,
                        "meta",
                        "Meta Concluída",
                        $"Parabéns! A meta \"{meta.Nome}\" foi concluída com sucesso.",
                        "/metas",
                        destaque: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar notificação de meta concluída para o usuário {UsuarioId}", solicitanteId);
                }
            }

            _logger.LogInformation("Meta única criada com {QuantidadeTarefas} tarefas vinculadas: MetaId {MetaId}, Nome '{Nome}', UsuarioId {UsuarioId}",
                tarefaIds.Count, meta.Id, meta.Nome, solicitanteId);

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

        public async Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorTarefaAsync(int tarefaId, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var metas = await _metaRepository.ObterMetasPorTarefaIdAsync(tarefaId);
            return metas.Select(MapToConsultaDTO);
        }

        public async Task<ConsultaMetaDTO> AtualizarMetaAsync(int id, AtualizarMetaDTO dto, int solicitanteId)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(id);
            if (meta == null)
                throw new KeyNotFoundException("Meta não encontrada.");

            var eraConcluida = meta.Concluida == true;

            var tarefa = meta.Tarefa ?? await _tarefaRepository.ObterTarefaPorIdAsync(meta.TarefaId);
            var ehAdminOuDono = tarefa != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (meta.UsuarioId != solicitanteId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para alterar esta meta.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Nome))
                meta.Nome = dto.Nome.Trim();

            if (dto.TarefaIds != null)
            {
                var novasTarefasIds = dto.TarefaIds.Distinct().Where(tId => tId > 0).ToList();
                if (!novasTarefasIds.Any())
                {
                    throw new ArgumentException("A meta deve estar vinculada a no mínimo uma tarefa válida.");
                }

                var tarefas = new List<Tarefa>();
                foreach (var tId in novasTarefasIds)
                {
                    var tarefaExistente = await _tarefaRepository.ObterTarefaPorIdAsync(tId);
                    if (tarefaExistente == null)
                    {
                        throw new ArgumentException($"A tarefa #{tId} vinculada não foi encontrada ou não é uma tarefa válida.");
                    }
                    tarefas.Add(tarefaExistente);
                }

                meta.TarefaId = novasTarefasIds.First();
                meta.Tarefa = tarefas.First();

                var paraRemover = meta.MetasTarefas.Where(mt => !novasTarefasIds.Contains(mt.TarefaId)).ToList();
                foreach (var item in paraRemover)
                {
                    meta.MetasTarefas.Remove(item);
                }

                var tarefasAtuaisIds = meta.MetasTarefas.Select(mt => mt.TarefaId).ToHashSet();
                foreach (var tId in novasTarefasIds)
                {
                    if (!tarefasAtuaisIds.Contains(tId))
                    {
                        meta.MetasTarefas.Add(new MetaTarefa
                        {
                            MetaId = meta.Id,
                            TarefaId = tId,
                            Tarefa = tarefas.First(t => t.Id == tId)
                        });
                    }
                }

                meta.Concluida = tarefas.All(t => t.Status == Status.Concluida);
            }
            else if (dto.TarefaId > 0 && dto.TarefaId != meta.TarefaId)
            {
                // RN07: Validar se a nova tarefa existe
                var novaTarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
                if (novaTarefa == null)
                    throw new ArgumentException("A nova tarefa vinculada não foi encontrada.");

                meta.TarefaId = dto.TarefaId;
                meta.Tarefa = novaTarefa;

                var paraRemover = meta.MetasTarefas.Where(mt => mt.TarefaId != dto.TarefaId).ToList();
                foreach (var item in paraRemover)
                {
                    meta.MetasTarefas.Remove(item);
                }

                if (!meta.MetasTarefas.Any(mt => mt.TarefaId == dto.TarefaId))
                {
                    meta.MetasTarefas.Add(new MetaTarefa
                    {
                        MetaId = meta.Id,
                        TarefaId = dto.TarefaId,
                        Tarefa = novaTarefa
                    });
                }

                // RN08: Checar conclusão
                if (novaTarefa.Status == Status.Concluida)
                    meta.Concluida = true;
            }

            if (dto.LimparDataFechamento)
            {
                meta.DataFechamento = null;
            }
            else if (dto.DataFechamento.HasValue)
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

            if (dto.Concluida.HasValue && dto.TarefaIds == null)
            {
                meta.Concluida = dto.Concluida.Value;
            }

            await _metaRepository.AtualizarMetaAsync(meta);

            if (!eraConcluida && meta.Concluida == true && _notificacaoService != null && meta.UsuarioId > 0)
            {
                try
                {
                    await _notificacaoService.CriarNotificacaoAsync(
                        meta.UsuarioId,
                        "meta",
                        "Meta Concluída",
                        $"Parabéns! A meta \"{meta.Nome}\" foi concluída com sucesso.",
                        "/metas",
                        destaque: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar notificação de meta concluída para o usuário {UsuarioId}", meta.UsuarioId);
                }
            }

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

                    if (_notificacaoService != null && meta.UsuarioId > 0)
                    {
                        try
                        {
                            await _notificacaoService.CriarNotificacaoAsync(
                                meta.UsuarioId,
                                "meta",
                                "Meta Concluída",
                                $"Parabéns! A meta \"{meta.Nome}\" foi concluída com sucesso.",
                                "/metas",
                                destaque: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de meta concluída para o usuário {UsuarioId}", meta.UsuarioId);
                        }
                    }
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
                    CriadorId = meta.Tarefa.CriadorId,
                    DataCriacao = meta.Tarefa.DataCriacao
                } : null,
                Tarefas = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
                    ? meta.MetasTarefas
                        .Where(mt => mt.Tarefa != null)
                        .Select(mt => new ConsultaTarefaDTO
                        {
                            Id = mt.Tarefa.Id,
                            Titulo = mt.Tarefa.Titulo,
                            Descricao = mt.Tarefa.Descricao,
                            DataVencimento = mt.Tarefa.DataVencimento,
                            Pontuacao = mt.Tarefa.Pontuacao,
                            Status = mt.Tarefa.Status,
                            Prioridade = mt.Tarefa.Prioridade,
                            CriadorId = mt.Tarefa.CriadorId,
                            DataCriacao = mt.Tarefa.DataCriacao
                        }).ToList()
                    : (meta.Tarefa != null ? new List<ConsultaTarefaDTO>
                    {
                        new ConsultaTarefaDTO
                        {
                            Id = meta.Tarefa.Id,
                            Titulo = meta.Tarefa.Titulo,
                            Descricao = meta.Tarefa.Descricao,
                            DataVencimento = meta.Tarefa.DataVencimento,
                            Pontuacao = meta.Tarefa.Pontuacao,
                            Status = meta.Tarefa.Status,
                            Prioridade = meta.Tarefa.Prioridade,
                            CriadorId = meta.Tarefa.CriadorId,
                            DataCriacao = meta.Tarefa.DataCriacao
                        }
                    } : new List<ConsultaTarefaDTO>())
            };
        }
    }
}
