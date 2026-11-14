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
        private readonly IEquipeRepository? _equipeRepository;
        private readonly IMembroEquipeRepository? _membroEquipeRepository;
        private readonly ILogger<MetaService> _logger;

        public MetaService(
            IMetaRepository metaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeAuthorizationService authService,
            ILogger<MetaService> logger,
            INotificacaoService? notificacaoService = null,
            IEquipeRepository? equipeRepository = null,
            IMembroEquipeRepository? membroEquipeRepository = null)
        {
            _metaRepository = metaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _authService = authService;
            _logger = logger;
            _notificacaoService = notificacaoService;
            _equipeRepository = equipeRepository;
            _membroEquipeRepository = membroEquipeRepository;
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

            var targetUsuarioId = solicitanteId;
            if (dto.UsuarioId.HasValue && dto.UsuarioId.Value > 0 && dto.UsuarioId.Value != solicitanteId)
            {
                var usuarioExiste = await _usuarioRepository.UsuarioExisteAsync(dto.UsuarioId.Value);
                if (!usuarioExiste)
                {
                    throw new KeyNotFoundException("O usuário integrante informado não foi encontrado.");
                }

                var ehAdminOuDono = await _authService.EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(solicitanteId, dto.UsuarioId.Value);
                if (!ehAdminOuDono)
                {
                    throw new ForbiddenException("Apenas o Dono ou Administradores da equipe possuem permissão para atribuir metas a integrantes.");
                }

                targetUsuarioId = dto.UsuarioId.Value;
            }

            if (dto.EquipeId.HasValue && dto.EquipeId.Value > 0)
            {
                if (_equipeRepository != null && !await _equipeRepository.EquipeExisteAsync(dto.EquipeId.Value))
                {
                    throw new KeyNotFoundException("Equipe informada não foi encontrada.");
                }

                var ehAdminOuDonoEquipe = await _authService.EhAdminOuDonoDaEquipeAsync(solicitanteId, dto.EquipeId.Value);
                if (!ehAdminOuDonoEquipe)
                {
                    throw new ForbiddenException("Apenas o Dono ou Administradores da equipe possuem permissão para vincular a meta à equipe.");
                }
            }

            // RN08: Concluida se TODAS as tarefas vinculadas estiverem Concluidas
            var todasConcluidas = tarefas.All(t => t.Status == Status.Concluida);

            var meta = new Meta
            {
                Nome = dto.Nome,
                DataFechamento = dataFechamentoUtc,
                Concluida = todasConcluidas,
                UsuarioId = targetUsuarioId
            };

            foreach (var tId in tarefaIds)
            {
                meta.MetasTarefas.Add(new MetaTarefa
                {
                    TarefaId = tId,
                    Tarefa = tarefas.First(t => t.Id == tId)
                });
            }

            if (dto.EquipeId.HasValue && dto.EquipeId.Value > 0)
            {
                // RN: O XP de meta de equipe deve iniciar em 0 e a cada tarefa vinculada, a recompensa de XP deve ser aumentada em 5
                var xpCalculado = tarefaIds.Count * 5;
                var xpFinal = (dto.XpRecompensa.HasValue && dto.XpRecompensa.Value > 0) ? dto.XpRecompensa.Value : xpCalculado;

                meta.MetasEquipes.Add(new MetaEquipe
                {
                    EquipeId = dto.EquipeId.Value,
                    XpRecompensa = xpFinal
                });
            }

            await _metaRepository.AdicionarMetaAsync(meta);

            if (targetUsuarioId != solicitanteId && _notificacaoService != null && targetUsuarioId > 0)
            {
                try
                {
                    await _notificacaoService.CriarNotificacaoAsync(
                        targetUsuarioId,
                        "meta",
                        "Nova Meta Atribuída",
                        $"Você recebeu uma nova meta: \"{meta.Nome}\".",
                        "/metas",
                        destaque: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar notificação de nova meta para UsuarioId {UsuarioId}", targetUsuarioId);
                }
            }

            if (dto.EquipeId.HasValue && dto.EquipeId.Value > 0 && _membroEquipeRepository != null && _notificacaoService != null)
            {
                try
                {
                    var membrosEquipe = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(dto.EquipeId.Value);
                    foreach (var membro in membrosEquipe)
                    {
                        if (membro.UsuarioId == solicitanteId || membro.UsuarioId == targetUsuarioId)
                            continue;

                        await _notificacaoService.CriarNotificacaoAsync(
                            membro.UsuarioId,
                            "meta",
                            "Nova Meta de Equipe",
                            $"Uma nova meta de equipe \"{meta.Nome}\" foi atribuída aos integrantes da equipe.",
                            "/metas",
                            destaque: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao notificar membros da equipe {EquipeId} sobre nova meta", dto.EquipeId.Value);
                }
            }

            if (meta.Concluida == true)
            {
                await ProcessarXpConclusaoMetaEquipeAsync(meta);

                if (_notificacaoService != null && targetUsuarioId > 0)
                {
                    try
                    {
                        await _notificacaoService.CriarNotificacaoAsync(
                            targetUsuarioId,
                            "meta",
                            "Meta Concluída",
                            $"Parabéns! A meta \"{meta.Nome}\" foi concluída com sucesso.",
                            "/metas",
                            destaque: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao criar notificação de meta concluída para o usuário {UsuarioId}", targetUsuarioId);
                    }
                }
            }

            _logger.LogInformation("Meta única criada com {QuantidadeTarefas} tarefas vinculadas: MetaId {MetaId}, Nome '{Nome}', UsuarioId {UsuarioId}",
                tarefaIds.Count, meta.Id, meta.Nome, targetUsuarioId);

            var metaCompleta = await _metaRepository.ObterMetaPorIdAsync(meta.Id);
            return MapToConsultaDTO(metaCompleta ?? meta);
        }

        public async Task<ConsultaMetaDTO?> ObterMetaPorIdAsync(int id, int solicitanteId)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(id);
            if (meta == null)
                return null;

            // RN09: Usuários comuns só podem visualizar suas próprias metas ou metas atribuídas à sua equipe
            var tarefasDaMeta = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
                ? meta.MetasTarefas.Where(mt => mt.Tarefa != null).Select(mt => mt.Tarefa).ToList()
                : (await _metaRepository.ObterTarefasDaMetaAsync(meta.Id)) ?? new List<Tarefa>();

            var ehAdminOuDono = false;
            foreach (var t in tarefasDaMeta)
            {
                if (t != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, t))
                {
                    ehAdminOuDono = true;
                    break;
                }
            }

            var ehMembroDaEquipe = meta.MetasEquipes != null && meta.MetasEquipes.Any(me =>
                me.Equipe != null && me.Equipe.Membros != null && me.Equipe.Membros.Any(m => m.UsuarioId == solicitanteId));

            if (!ehMembroDaEquipe && _membroEquipeRepository != null && meta.MetasEquipes != null && meta.MetasEquipes.Any())
            {
                foreach (var me in meta.MetasEquipes)
                {
                    var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(me.EquipeId, solicitanteId);
                    if (membro != null)
                    {
                        ehMembroDaEquipe = true;
                        break;
                    }
                }
            }

            if (meta.UsuarioId != solicitanteId && !ehAdminOuDono && !ehMembroDaEquipe)
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

        public async Task<IEnumerable<ConsultaMetaDTO>> ListarMetasPorEquipeAsync(int equipeId, int solicitanteId)
        {
            if (_equipeRepository != null && !await _equipeRepository.EquipeExisteAsync(equipeId))
                throw new KeyNotFoundException("Equipe informada não foi encontrada.");

            if (_membroEquipeRepository != null)
            {
                var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId);
                if (membro == null)
                {
                    throw new ForbiddenException("Você não possui permissão para visualizar as metas desta equipe.");
                }
            }

            var metas = await _metaRepository.ObterMetasPorEquipeIdAsync(equipeId);
            return metas.Select(MapToConsultaDTO);
        }

        public async Task<ConsultaMetaDTO> AtribuirEquipeAsync(int metaId, int equipeId, int solicitanteId, int? xpRecompensa = null)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(metaId);
            if (meta == null)
                throw new KeyNotFoundException("Meta não encontrada.");

            if (_equipeRepository != null && !await _equipeRepository.EquipeExisteAsync(equipeId))
                throw new KeyNotFoundException("Equipe informada não foi encontrada.");

            var ehAdminOuDono = await _authService.EhAdminOuDonoDaEquipeAsync(solicitanteId, equipeId);
            if (!ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o Dono ou Administradores da equipe possuem permissão para atribuir metas à equipe.");
            }

            var jaVinculada = await _metaRepository.MetaEquipeExisteAsync(metaId, equipeId);
            if (!jaVinculada)
            {
                var tarefasDaMeta = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
                    ? meta.MetasTarefas.Count
                    : (await _metaRepository.ObterTarefasDaMetaAsync(metaId))?.Count ?? 0;

                var xpCalculado = tarefasDaMeta * 5;
                var xpFinal = (xpRecompensa.HasValue && xpRecompensa.Value > 0) ? xpRecompensa.Value : xpCalculado;

                await _metaRepository.AdicionarMetaEquipeAsync(new MetaEquipe
                {
                    MetaId = metaId,
                    EquipeId = equipeId,
                    XpRecompensa = xpFinal
                });
            }

            if (_membroEquipeRepository != null)
            {
                var membroSolicitante = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId);
                var membros = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(equipeId);

                foreach (var membro in membros)
                {
                    if (membro.UsuarioId == solicitanteId)
                        continue;

                    if (membroSolicitante != null && ObterNivelCargo(membroSolicitante.Cargo) <= ObterNivelCargo(membro.Cargo))
                        continue;

                    if (_notificacaoService != null)
                    {
                        try
                        {
                            await _notificacaoService.CriarNotificacaoAsync(
                                membro.UsuarioId,
                                "meta",
                                "Nova Meta de Equipe",
                                $"Uma nova meta de equipe \"{meta.Nome}\" foi atribuída aos integrantes da equipe.",
                                "/metas",
                                destaque: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao notificar membro {UsuarioId} sobre meta de equipe {MetaId}", membro.UsuarioId, meta.Id);
                        }
                    }
                }
            }

            _logger.LogInformation("Meta {MetaId} atribuída à Equipe {EquipeId} por SolicitanteId {SolicitanteId}",
                metaId, equipeId, solicitanteId);

            var metaAtualizada = await _metaRepository.ObterMetaPorIdAsync(metaId);
            return MapToConsultaDTO(metaAtualizada ?? meta);
        }

        public async Task<ConsultaMetaDTO> AtualizarMetaAsync(int id, AtualizarMetaDTO dto, int solicitanteId)
        {
            var meta = await _metaRepository.ObterMetaPorIdAsync(id);
            if (meta == null)
                throw new KeyNotFoundException("Meta não encontrada.");

            var eraConcluida = meta.Concluida == true;

            var tarefasDaMeta = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
                ? meta.MetasTarefas.Where(mt => mt.Tarefa != null).Select(mt => mt.Tarefa).ToList()
                : (await _metaRepository.ObterTarefasDaMetaAsync(meta.Id)) ?? new List<Tarefa>();

            var ehAdminOuDono = false;
            foreach (var t in tarefasDaMeta)
            {
                if (t != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, t))
                {
                    ehAdminOuDono = true;
                    break;
                }
            }

            if (meta.UsuarioId != solicitanteId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para alterar esta meta.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Nome))
                meta.Nome = dto.Nome.Trim();

            var novasTarefasIds = (dto.TarefaIds != null)
                ? dto.TarefaIds.Distinct().Where(tId => tId > 0).ToList()
                : (dto.TarefaId > 0 ? new List<int> { dto.TarefaId } : null);

            if (novasTarefasIds != null)
            {
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

                meta.MetasTarefas ??= new List<MetaTarefa>();
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

                // Atualiza XpRecompensa das equipes vinculadas com base na quantidade de tarefas (inicia em 0, +5 por tarefa)
                if (meta.MetasEquipes != null && meta.MetasEquipes.Any())
                {
                    var novoXp = novasTarefasIds.Count * 5;
                    foreach (var me in meta.MetasEquipes)
                    {
                        me.XpRecompensa = novoXp;
                    }
                }
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

            if (!eraConcluida && meta.Concluida == true)
            {
                await ProcessarXpConclusaoMetaEquipeAsync(meta);

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

            var tarefasDaMeta = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
                ? meta.MetasTarefas.Where(mt => mt.Tarefa != null).Select(mt => mt.Tarefa).ToList()
                : (await _metaRepository.ObterTarefasDaMetaAsync(meta.Id)) ?? new List<Tarefa>();

            var ehAdminOuDono = false;
            foreach (var t in tarefasDaMeta)
            {
                if (t != null && await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, t))
                {
                    ehAdminOuDono = true;
                    break;
                }
            }

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

                    await ProcessarXpConclusaoMetaEquipeAsync(meta);

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

        public static int ObterLimiteXpDoNivel(int nivel)
        {
            var nivelEfetivo = Math.Max(1, nivel);
            return nivelEfetivo * 100;
        }

        public static void IncrementarXpEAtualizarNivel(Usuario usuario, int xpGanho)
        {
            if (xpGanho <= 0)
                return;

            if (usuario.Nivel <= 0)
                usuario.Nivel = 1;

            if (usuario.Experiencia < 0)
                usuario.Experiencia = 0;

            usuario.Experiencia += xpGanho;

            while (usuario.Experiencia >= ObterLimiteXpDoNivel(usuario.Nivel))
            {
                usuario.Experiencia -= ObterLimiteXpDoNivel(usuario.Nivel);
                usuario.Nivel += 1;
            }
        }

        public static void DecrementarXp(Usuario usuario, int xpPerdido)
        {
            if (xpPerdido <= 0)
                return;

            if (usuario.Experiencia < 0)
                usuario.Experiencia = 0;

            usuario.Experiencia = Math.Max(0, usuario.Experiencia - xpPerdido);
        }

        public async Task<int> VerificarMetasExpiradasAsync()
        {
            var agoraUtc = DateTime.UtcNow;

            var metas = await _metaRepository.ObterTodasMetasAsync();
            var metasExpiradasNaoConcluidas = metas
                .Where(m => m.DataFechamento.HasValue &&
                            m.DataFechamento.Value < agoraUtc &&
                            m.Concluida != true &&
                            !m.PenalidadeAplicada)
                .ToList();

            if (!metasExpiradasNaoConcluidas.Any())
            {
                return 0;
            }

            var totalPenalizadas = 0;
            var metasPorUsuario = metasExpiradasNaoConcluidas.GroupBy(m => m.UsuarioId);

            foreach (var grupo in metasPorUsuario)
            {
                var usuarioId = grupo.Key;
                if (usuarioId <= 0) continue;

                var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
                if (usuario == null) continue;

                var metasEquipeDoUsuario = new List<Meta>();
                var metasIndividuaisDoUsuario = new List<Meta>();

                foreach (var meta in grupo)
                {
                    var tarefasDaMeta = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
                        ? meta.MetasTarefas.Where(mt => mt.Tarefa != null).Select(mt => mt.Tarefa).ToList()
                        : (await _metaRepository.ObterTarefasDaMetaAsync(meta.Id)) ?? new List<Tarefa>();

                    var todasConcluidas = tarefasDaMeta.Count > 0 && tarefasDaMeta.All(t => t.Status == Status.Concluida);

                    if (todasConcluidas)
                    {
                        meta.Concluida = true;
                        await _metaRepository.AtualizarMetaAsync(meta);
                        await ProcessarXpConclusaoMetaEquipeAsync(meta);
                    }
                    else
                    {
                        // RN: Somente metas de equipe devem possuir atribuição e remoção de XP.
                        // Metas individuais não devem alterar o saldo de XP quando expiradas.
                        var ehMetaDeEquipe = meta.MetasEquipes != null && meta.MetasEquipes.Any();
                        if (ehMetaDeEquipe)
                        {
                            metasEquipeDoUsuario.Add(meta);
                        }
                        else
                        {
                            metasIndividuaisDoUsuario.Add(meta);
                        }
                    }
                }

                // Processa metas individuais expiradas (apenas marca flag de verificação sem aplicar penalidade de XP)
                foreach (var meta in metasIndividuaisDoUsuario)
                {
                    meta.PenalidadeAplicada = true;
                    await _metaRepository.AtualizarMetaAsync(meta);
                }

                // Processa metas de equipe expiradas (aplica penalidade de XP e emite notificação com identidade de meta)
                if (metasEquipeDoUsuario.Any())
                {
                    var quantidadeNaoConcluidas = metasEquipeDoUsuario.Count;
                    var xpRemovido = quantidadeNaoConcluidas * 5;

                    var xpAnterior = usuario.Experiencia;
                    DecrementarXp(usuario, xpRemovido);
                    await _usuarioRepository.AtualizarUsuarioAsync(usuario);

                    foreach (var meta in metasEquipeDoUsuario)
                    {
                        meta.PenalidadeAplicada = true;
                        await _metaRepository.AtualizarMetaAsync(meta);
                        totalPenalizadas++;
                    }

                    _logger.LogWarning(
                        "Penalidade de XP aplicada ao UsuarioId {UsuarioId}: -{XpRemovido} XP ({TotalMetas} meta(s) de equipe não concluída(s) com prazo expirado). Saldo: {XpAnterior} -> {NovoXp} XP",
                        usuario.Id, xpRemovido, quantidadeNaoConcluidas, xpAnterior, usuario.Experiencia);

                    if (_notificacaoService != null)
                    {
                        try
                        {
                            await _notificacaoService.CriarNotificacaoAsync(
                                usuario.Id,
                                "meta",
                                "Penalidade de XP: Meta Expirada",
                                $"Você teve {quantidadeNaoConcluidas} meta(s) de equipe não concluída(s) com prazo expirado. Foram deduzidos {xpRemovido} XP do seu saldo.",
                                "/metas",
                                destaque: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de penalidade de XP para o usuário {UsuarioId}", usuario.Id);
                        }
                    }
                }
            }

            return totalPenalizadas;
        }

        private async Task ProcessarXpConclusaoMetaEquipeAsync(Meta meta)
        {
            // Metas puramente individuais não devem gerar acúmulo de XP.
            if (meta.MetasEquipes == null || !meta.MetasEquipes.Any())
            {
                _logger.LogInformation("Meta {MetaId} é puramente individual. Nenhum XP concedido.", meta.Id);
                return;
            }

            var xpGanho = meta.MetasEquipes.Sum(me => me.XpRecompensa);
            if (xpGanho <= 0)
            {
                _logger.LogInformation("Meta de equipe {MetaId} concluída, mas com XpRecompensa zerado.", meta.Id);
                return;
            }

            var usuario = meta.Usuario;
            if (usuario == null && _usuarioRepository != null && meta.UsuarioId > 0)
            {
                usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(meta.UsuarioId);
            }

            if (usuario == null)
            {
                _logger.LogWarning("Não foi possível localizar o usuário {UsuarioId} para conceder XP da meta {MetaId}.", meta.UsuarioId, meta.Id);
                return;
            }

            var nivelAnterior = usuario.Nivel;
            var xpAnterior = usuario.Experiencia;

            IncrementarXpEAtualizarNivel(usuario, xpGanho);

            if (_usuarioRepository != null)
            {
                await _usuarioRepository.AtualizarUsuarioAsync(usuario);
            }

            _logger.LogInformation(
                "XP concedido ao usuário {UsuarioId} pela meta de equipe {MetaId}: +{XpGanho} XP. Nível: {NivelAnterior} -> {NovoNivel}. Saldo XP: {XpAnterior} -> {NovoXp}",
                usuario.Id, meta.Id, xpGanho, nivelAnterior, usuario.Nivel, xpAnterior, usuario.Experiencia);
        }

        private static int ObterNivelCargo(Cargo cargo)
        {
            return cargo switch
            {
                Cargo.Dono => 3,
                Cargo.Admin => 2,
                Cargo.Membro => 1,
                _ => 0
            };
        }

        private static ConsultaMetaDTO MapToConsultaDTO(Meta meta)
        {
            var tarefasLista = (meta.MetasTarefas != null && meta.MetasTarefas.Any())
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
                : new List<ConsultaTarefaDTO>();

            var primeiraTarefa = tarefasLista.FirstOrDefault();

            return new ConsultaMetaDTO
            {
                Id = meta.Id,
                Nome = meta.Nome,
                DataFechamento = meta.DataFechamento,
                Concluida = meta.Concluida ?? false,
                TarefaId = primeiraTarefa?.Id ?? 0,
                UsuarioId = meta.UsuarioId,
                Usuario = meta.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = meta.Usuario.Id,
                    Nome = meta.Usuario.Nome,
                    Email = meta.Usuario.Email,
                    Telefone = meta.Usuario.Telefone,
                    EmailVerificado = meta.Usuario.EmailVerificado,
                    Nivel = meta.Usuario.Nivel,
                    Experiencia = meta.Usuario.Experiencia,
                    FotoPerfil = meta.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = meta.Usuario.FotoPerfil.Id,
                        UrlCaminho = meta.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = meta.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = meta.Usuario.FotoPerfil.MimeType,
                        DataEnvio = meta.Usuario.FotoPerfil.DataEnvio
                    } : null
                } : null,
                Tarefa = primeiraTarefa,
                Tarefas = tarefasLista,
                Equipes = meta.MetasEquipes?
                    .Where(me => me.Equipe != null)
                    .Select(me => new ConsultaEquipeDTO
                    {
                        Id = me.Equipe.Id,
                        Nome = me.Equipe.Nome,
                        DataCriacao = me.Equipe.DataCriacao
                    }).ToList() ?? new List<ConsultaEquipeDTO>(),
                XpRecompensa = meta.MetasEquipes?.Sum(me => me.XpRecompensa),
                PenalidadeAplicada = meta.PenalidadeAplicada
            };
        }
    }
}
