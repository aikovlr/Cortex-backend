using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Cortex.Services.Implementations
{
    public class TarefaService : ITarefaService
    {
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IMetaService _metaService;
        private readonly IEquipeAuthorizationService _authService;
        private readonly IArquivoValidacaoService _arquivoValidacaoService;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificacaoService? _notificacaoService;
        private readonly IRespostaTarefaRepository? _respostaRepository;
        private readonly ILogger<TarefaService> _logger;

        public TarefaService(
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IMetaService metaService,
            IEquipeAuthorizationService authService,
            IArquivoValidacaoService arquivoValidacaoService,
            IWebHostEnvironment environment,
            ILogger<TarefaService> logger,
            INotificacaoService? notificacaoService = null,
            IRespostaTarefaRepository? respostaRepository = null)
        {
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _metaService = metaService;
            _authService = authService;
            _arquivoValidacaoService = arquivoValidacaoService;
            _environment = environment;
            _logger = logger;
            _notificacaoService = notificacaoService;
            _respostaRepository = respostaRepository;
        }

        public async Task<ConsultaTarefaDTO> CriarTarefaAsync(CriarTarefaDTO dto, int criadorId)
        {
            var criador = await _usuarioRepository.ObterUsuarioPorIdAsync(criadorId);
            if (criador == null)
                throw new KeyNotFoundException("Usuário criador não encontrado.");

            var dataVencimentoUtc = dto.DataVencimento.ToUniversalTime();

            var tarefa = new Tarefa
            {
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                DataVencimento = dataVencimentoUtc,
                Pontuacao = dto.Pontuacao,
                Status = Status.Pendente,
                Prioridade = dto.Prioridade,
                DataCriacao = DateTime.UtcNow,
                CriadorId = criadorId,
                Criador = criador
            };

            if (dto.Anexos != null && dto.Anexos.Count > 0)
            {
                foreach (var arquivo in dto.Anexos)
                {
                    var anexo = await SalvarAnexoFisicoAsync(arquivo);
                    tarefa.Anexos.Add(anexo);
                }
            }

            await _tarefaRepository.AdicionarTarefaAsync(tarefa);

            _logger.LogInformation("Auditoria: Tarefa criada. TarefaId: {TarefaId}, CriadorId: {CriadorId}, Titulo: {Titulo}",
                tarefa.Id, criadorId, tarefa.Titulo);

            return MapToConsultaDTO(tarefa);
        }

        public async Task<DetalhesTarefaDTO?> ObterTarefaPorIdAsync(int id, int? solicitanteId = null)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                return null;

            // Garante que o responsável único possua uma RespostaTarefa quando a tarefa estiver concluída
            if (tarefa.Status == Status.Concluida && _respostaRepository != null)
            {
                var respostasExistentes = tarefa.Respostas ?? new List<RespostaTarefa>();

                int? usuarioAlvo = null;
                if (tarefa.ResponsaveisTarefas != null && tarefa.ResponsaveisTarefas.Count == 1)
                {
                    usuarioAlvo = tarefa.ResponsaveisTarefas.First().UsuarioId;
                }
                else if (tarefa.CriadorId > 0 && (tarefa.ResponsaveisTarefas == null || !tarefa.ResponsaveisTarefas.Any()))
                {
                    usuarioAlvo = tarefa.CriadorId;
                }

                if (usuarioAlvo.HasValue && !respostasExistentes.Any(r => r.UsuarioId == usuarioAlvo.Value))
                {
                    var novaResposta = new RespostaTarefa
                    {
                        TarefaId = id,
                        UsuarioId = usuarioAlvo.Value,
                        DataResposta = DateTime.UtcNow
                    };
                    await _respostaRepository.AdicionarRespostaTarefaAsync(novaResposta);
                    tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id) ?? tarefa;
                }
            }

            return MapToDetalhesDTO(tarefa, solicitanteId);
        }

        public async Task<IEnumerable<ConsultaTarefaDTO>> ListarTarefasAsync(FiltroTarefasDTO? filtros = null, int? solicitanteId = null)
        {
            var dataInicioUtc = filtros?.DataInicio?.ToUniversalTime();
            var dataFimUtc = filtros?.DataFim?.ToUniversalTime();

            var tarefas = await _tarefaRepository.ObterTarefasComFiltroAsync(
                status: filtros?.Status,
                prioridade: filtros?.Prioridade,
                responsavelId: filtros?.ResponsavelId,
                criadorId: filtros?.CriadorId,
                dataInicio: dataInicioUtc,
                dataFim: dataFimUtc,
                termoBusca: filtros?.TermoBusca);

            // Se a listagem for pela visão do criador (tarefas atribuídas), exibe o status global da tarefa
            var solicitanteEfetivo = filtros?.CriadorId.HasValue == true && !filtros.ResponsavelId.HasValue
                ? null
                : solicitanteId;

            return tarefas.Select(t => MapToConsultaDTO(t, solicitanteEfetivo));
        }

        public async Task<DetalhesTarefaDTO> AtualizarTarefaAsync(int id, AtualizarTarefaDTO dto, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            // Apenas o criador ou Dono/Admin da equipe possuem autorização para alterar a tarefa
            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador da tarefa ou administradores da equipe possuem permissão para editá-la.");
            }

            var camposAlterados = new List<string>();

            // Aplicar campos somente se enviados (patch semântico)
            if (!string.IsNullOrWhiteSpace(dto.Titulo) && tarefa.Titulo != dto.Titulo)
            {
                tarefa.Titulo = dto.Titulo;
                camposAlterados.Add("Titulo");
            }

            if (dto.Descricao != null && tarefa.Descricao != dto.Descricao) // null = não alterar; "" = limpar
            {
                tarefa.Descricao = dto.Descricao;
                camposAlterados.Add("Descricao");
            }

            if (dto.DataVencimento.HasValue)
            {
                var dataUtc = dto.DataVencimento.Value.Kind == DateTimeKind.Utc
                    ? dto.DataVencimento.Value
                    : dto.DataVencimento.Value.ToUniversalTime();

                if (tarefa.DataVencimento != dataUtc)
                {
                    tarefa.DataVencimento = dataUtc;
                    camposAlterados.Add("DataVencimento");
                }
            }

            if (dto.Pontuacao.HasValue && tarefa.Pontuacao != dto.Pontuacao.Value)
            {
                tarefa.Pontuacao = dto.Pontuacao.Value;
                camposAlterados.Add("Pontuacao");
            }

            if (dto.Prioridade.HasValue && tarefa.Prioridade != dto.Prioridade.Value)
            {
                tarefa.Prioridade = dto.Prioridade.Value;
                camposAlterados.Add("Prioridade");
            }

            if (dto.Anexos != null && dto.Anexos.Count > 0)
            {
                foreach (var arquivo in dto.Anexos)
                {
                    var anexo = await SalvarAnexoFisicoAsync(arquivo);
                    tarefa.Anexos.Add(anexo);
                }
                camposAlterados.Add("Anexos");
            }

            await _tarefaRepository.AtualizarTarefaAsync(tarefa);

            // Notifica os responsáveis sobre as alterações na tarefa
            if (_notificacaoService != null && tarefa.ResponsaveisTarefas != null && camposAlterados.Count > 0)
            {
                foreach (var resp in tarefa.ResponsaveisTarefas)
                {
                    if (resp.UsuarioId > 0 && resp.UsuarioId != solicitanteId)
                    {
                        try
                        {
                            await _notificacaoService.CriarNotificacaoAsync(
                                resp.UsuarioId,
                                "tarefa",
                                "Tarefa Atualizada",
                                $"A tarefa \"{tarefa.Titulo}\" foi alterada.",
                                $"/minhas-tarefas/{tarefa.Id}",
                                false,
                                tarefa.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de atualização para o responsável {UsuarioId}", resp.UsuarioId);
                        }
                    }
                }
            }

            _logger.LogInformation("Auditoria: Tarefa alterada. TarefaId: {TarefaId}, AlteradoPor: {AlteradoPor}, CamposAlterados: {CamposAlterados}",
                id, solicitanteId, camposAlterados);

            var tarefaAtualizada = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            return MapToDetalhesDTO(tarefaAtualizada ?? tarefa, solicitanteId);
        }

        public async Task<bool> DeletarTarefaAsync(int id, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                return false;

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador da tarefa ou administradores da equipe possuem permissão para excluí-la.");
            }

            var removida = await _tarefaRepository.RemoverTarefaAsync(id);

            if (removida)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "Tarefa", id, solicitanteId);
            }

            return removida;
        }

        public async Task<DetalhesTarefaDTO> ConcluirTarefaAsync(int id, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId);
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehResponsavel && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para concluir esta tarefa.");
            }

            // Garante que o solicitante que concluiu a tarefa possua uma RespostaTarefa para aparecer na lista de respostas
            if (_respostaRepository != null && solicitanteId > 0)
            {
                var jaPossuiResposta = tarefa.Respostas != null && tarefa.Respostas.Any(r => r.UsuarioId == solicitanteId);
                if (!jaPossuiResposta)
                {
                    var novaResposta = new RespostaTarefa
                    {
                        TarefaId = id,
                        UsuarioId = solicitanteId,
                        DataResposta = DateTime.UtcNow
                    };
                    await _respostaRepository.AdicionarRespostaTarefaAsync(novaResposta);
                    tarefa.Respostas?.Add(novaResposta);
                }
            }

            // Se a tarefa tiver múltiplos responsáveis, apenas marca a tarefa como concluída se todos tiverem concluído
            var todosConcluiram = true;
            if (tarefa.ResponsaveisTarefas != null && tarefa.ResponsaveisTarefas.Count > 1 && _respostaRepository != null)
            {
                var respostasAtualizadas = await _respostaRepository.ObterRespostasTarefaPorTarefaIdAsync(id);
                var idsQueResponderam = respostasAtualizadas != null
                    ? respostasAtualizadas.Select(r => r.UsuarioId).ToHashSet()
                    : new HashSet<int>();
                todosConcluiram = tarefa.ResponsaveisTarefas.All(r => idsQueResponderam.Contains(r.UsuarioId));
            }

            var deveMarcarConcluida = todosConcluiram || (!ehResponsavel && (ehCriador || ehAdminOuDono));

            if (deveMarcarConcluida)
            {
                tarefa.Status = Status.Concluida;
                await _tarefaRepository.AtualizarTarefaAsync(tarefa);
            }

            _logger.LogInformation("Auditoria: Tarefa concluída. TarefaId: {TarefaId}, ConcluidaPor: {ConcluidaPor}, Timestamp: {Timestamp}",
                id, solicitanteId, DateTime.UtcNow);

            // Notifica o criador da tarefa sobre a conclusão (com stacking para não poluir a aba do criador)
            if (_notificacaoService != null && tarefa.CriadorId > 0 && tarefa.CriadorId != solicitanteId)
            {
                try
                {
                    // Identifica os responsáveis que já concluíram a tarefa
                    var idsConcluidos = new HashSet<int> { solicitanteId };
                    if (_respostaRepository != null)
                    {
                        var respostasDb = await _respostaRepository.ObterRespostasTarefaPorTarefaIdAsync(id);
                        if (respostasDb != null)
                        {
                            foreach (var r in respostasDb)
                            {
                                idsConcluidos.Add(r.UsuarioId);
                            }
                        }
                    }
                    else if (tarefa.Respostas != null)
                    {
                        foreach (var r in tarefa.Respostas)
                        {
                            idsConcluidos.Add(r.UsuarioId);
                        }
                    }

                    var nomesResponsaveisConcluidos = new List<string>();
                    if (tarefa.ResponsaveisTarefas != null && tarefa.ResponsaveisTarefas.Count > 0)
                    {
                        foreach (var resp in tarefa.ResponsaveisTarefas)
                        {
                            if (idsConcluidos.Contains(resp.UsuarioId))
                            {
                                var nome = resp.Usuario?.Nome;
                                if (string.IsNullOrWhiteSpace(nome))
                                {
                                    var u = await _usuarioRepository.ObterUsuarioPorIdAsync(resp.UsuarioId);
                                    nome = u?.Nome;
                                }

                                if (!string.IsNullOrWhiteSpace(nome) && !nomesResponsaveisConcluidos.Contains(nome))
                                {
                                    nomesResponsaveisConcluidos.Add(nome);
                                }
                            }
                        }
                    }

                    if (nomesResponsaveisConcluidos.Count == 0)
                    {
                        var u = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
                        if (u != null && !string.IsNullOrWhiteSpace(u.Nome))
                        {
                            nomesResponsaveisConcluidos.Add(u.Nome);
                        }
                    }

                    string titulo;
                    string mensagem;
                    var totalConcluidos = nomesResponsaveisConcluidos.Count;

                    if (totalConcluidos <= 1)
                    {
                        var autor = nomesResponsaveisConcluidos.FirstOrDefault();
                        titulo = "Tarefa Concluída";
                        mensagem = !string.IsNullOrWhiteSpace(autor)
                            ? $"O responsável {autor} concluiu a tarefa \"{tarefa.Titulo}\"."
                            : $"A tarefa \"{tarefa.Titulo}\" foi concluída/entregue.";
                    }
                    else if (todosConcluiram)
                    {
                        titulo = $"Tarefa Concluída ({totalConcluidos})";
                        mensagem = $"Todos os responsáveis ({string.Join(", ", nomesResponsaveisConcluidos)}) concluíram a tarefa \"{tarefa.Titulo}\".";
                    }
                    else if (totalConcluidos == 2)
                    {
                        titulo = "Tarefa Concluída (2)";
                        mensagem = $"Os responsáveis {nomesResponsaveisConcluidos[0]} e {nomesResponsaveisConcluidos[1]} concluíram a tarefa \"{tarefa.Titulo}\".";
                    }
                    else if (totalConcluidos == 3)
                    {
                        titulo = "Tarefa Concluída (3)";
                        mensagem = $"Os responsáveis {nomesResponsaveisConcluidos[0]}, {nomesResponsaveisConcluidos[1]} e {nomesResponsaveisConcluidos[2]} concluíram a tarefa \"{tarefa.Titulo}\".";
                    }
                    else
                    {
                        titulo = $"Tarefa Concluída ({totalConcluidos})";
                        mensagem = $"{nomesResponsaveisConcluidos[0]}, {nomesResponsaveisConcluidos[1]} e mais {totalConcluidos - 2} responsáveis concluíram a tarefa \"{tarefa.Titulo}\".";
                    }

                    await _notificacaoService.CriarNotificacaoAsync(
                        tarefa.CriadorId,
                        "tarefa",
                        titulo,
                        mensagem,
                        $"/tarefas-atribuidas/{tarefa.Id}",
                        false,
                        tarefa.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar notificação de conclusão para o criador {CriadorId}", tarefa.CriadorId);
                }
            }

            // Delega a verificação de metas ao MetaService (SRP — cada serviço cuida do seu domínio)
            await _metaService.VerificarConclusaoMetaAsync(id);

            var tarefaAtualizada = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            return MapToDetalhesDTO(tarefaAtualizada ?? tarefa, solicitanteId);
        }

        public async Task<DetalhesTarefaDTO> CancelarConclusaoTarefaAsync(int id, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId);
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehResponsavel && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para cancelar a entrega desta tarefa.");
            }

            // Ao cancelar a entrega/conclusão, remove a resposta automática gerada sem nota apenas do solicitante
            if (_respostaRepository != null && tarefa.Respostas != null)
            {
                var respostasSemAnexoENota = tarefa.Respostas
                    .Where(r => !r.Nota.HasValue && (r.Anexos == null || !r.Anexos.Any()))
                    .Where(r => r.UsuarioId == solicitanteId)
                    .ToList();

                foreach (var resp in respostasSemAnexoENota)
                {
                    await _respostaRepository.RemoverRespostaTarefaAsync(resp.Id);
                }
            }

            // Retorna para Atrasada se já passou do vencimento, caso contrário para Pendente
            tarefa.Status = tarefa.DataVencimento < DateTime.UtcNow ? Status.Atrasada : Status.Pendente;
            await _tarefaRepository.AtualizarTarefaAsync(tarefa);

            _logger.LogInformation("Auditoria: Conclusão da tarefa cancelada. TarefaId: {TarefaId}, CanceladaPor: {CanceladaPor}, NovoStatus: {NovoStatus}, Timestamp: {Timestamp}",
                id, solicitanteId, tarefa.Status, DateTime.UtcNow);

            // Notifica os responsáveis sobre o cancelamento da entrega
            if (_notificacaoService != null && tarefa.ResponsaveisTarefas != null)
            {
                foreach (var resp in tarefa.ResponsaveisTarefas)
                {
                    if (resp.UsuarioId != solicitanteId)
                    {
                        try
                        {
                            await _notificacaoService.CriarNotificacaoAsync(
                                resp.UsuarioId,
                                "tarefa",
                                "Conclusão de Tarefa Cancelada",
                                $"A entrega da tarefa \"{tarefa.Titulo}\" foi cancelada e ela voltou ao status pendente.",
                                $"/minhas-tarefas/{tarefa.Id}",
                                false,
                                tarefa.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de cancelamento de conclusão para {UsuarioId}", resp.UsuarioId);
                        }
                    }
                }
            }

            var tarefaAtualizada = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            return MapToDetalhesDTO(tarefaAtualizada ?? tarefa, solicitanteId);
        }

        public async Task<int> VerificarTarefasAtrasadasAsync()
        {
            var tarefasVencidas = await _tarefaRepository.ObterTarefasVencidasNaoConcluidasAsync(DateTime.UtcNow);
            var totalAtrasadas = 0;

            foreach (var tarefa in tarefasVencidas)
            {
                tarefa.Status = Status.Atrasada;
                await _tarefaRepository.AtualizarTarefaAsync(tarefa);
                totalAtrasadas++;

                _logger.LogWarning("Tarefa {TarefaId} ('{Titulo}') transitou automaticamente para Atrasada. Vencimento era: {DataVencimento}",
                    tarefa.Id, tarefa.Titulo, tarefa.DataVencimento);

                if (_notificacaoService != null && tarefa.ResponsaveisTarefas != null)
                {
                    foreach (var resp in tarefa.ResponsaveisTarefas)
                    {
                        try
                        {
                            await _notificacaoService.CriarNotificacaoAsync(
                                resp.UsuarioId,
                                "tarefa_vencida",
                                "Tarefa Atrasada",
                                $"Atenção: A data limite de entrega da tarefa \"{tarefa.Titulo}\" expirou e ela está atrasada.",
                                $"/minhas-tarefas/{tarefa.Id}",
                                true,
                                tarefa.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao criar notificação de tarefa atrasada para {UsuarioId}", resp.UsuarioId);
                        }
                    }
                }
            }

            return totalAtrasadas;
        }

        private async Task<Anexo> SalvarAnexoFisicoAsync(IFormFile arquivo)
        {
            _arquivoValidacaoService.ValidarAnexoTarefa(arquivo);

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "tarefas");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var nomeOriginalSanitizado = Path.GetFileName(arquivo.FileName);
            var extensao = Path.GetExtension(nomeOriginalSanitizado).ToLowerInvariant();
            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(uploadDir, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return new Anexo
            {
                NomeOriginal = arquivo.FileName,
                MimeType = arquivo.ContentType,
                UrlCaminho = $"/uploads/tarefas/{nomeArquivo}",
                TipoEntidade = TipoEntidade.Tarefa,
                DataEnvio = DateTime.UtcNow
            };
        }

        private static Status ObterStatusEfetivo(Tarefa tarefa, int? solicitanteId)
        {
            var statusEfetivo = tarefa.Status;

            if (solicitanteId.HasValue && tarefa.ResponsaveisTarefas != null && tarefa.ResponsaveisTarefas.Count > 1)
            {
                var solicitanteEhResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId.Value);
                if (solicitanteEhResponsavel)
                {
                    var solicitanteRespondeu = tarefa.Respostas != null && tarefa.Respostas.Any(r => r.UsuarioId == solicitanteId.Value);
                    if (!solicitanteRespondeu)
                    {
                        statusEfetivo = tarefa.DataVencimento < DateTime.UtcNow ? Status.Atrasada : Status.Pendente;
                    }
                    else
                    {
                        statusEfetivo = Status.Concluida;
                    }
                }
            }

            return statusEfetivo;
        }

        private static ConsultaTarefaDTO MapToConsultaDTO(Tarefa tarefa, int? solicitanteId = null)
        {
            return new ConsultaTarefaDTO
            {
                Id = tarefa.Id,
                Titulo = tarefa.Titulo,
                Descricao = tarefa.Descricao,
                DataVencimento = tarefa.DataVencimento,
                Pontuacao = tarefa.Pontuacao,
                Status = ObterStatusEfetivo(tarefa, solicitanteId),
                Prioridade = tarefa.Prioridade,
                CriadorId = tarefa.CriadorId,
                DataCriacao = tarefa.DataCriacao
            };
        }

        private static DetalhesTarefaDTO MapToDetalhesDTO(Tarefa tarefa, int? solicitanteId = null)
        {
            return new DetalhesTarefaDTO
            {
                Id = tarefa.Id,
                Titulo = tarefa.Titulo,
                Descricao = tarefa.Descricao,
                DataVencimento = tarefa.DataVencimento,
                Pontuacao = tarefa.Pontuacao,
                Status = ObterStatusEfetivo(tarefa, solicitanteId),
                Prioridade = tarefa.Prioridade,
                CriadorId = tarefa.CriadorId,
                DataCriacao = tarefa.DataCriacao,
                Criador = tarefa.Criador != null ? new ConsultaUsuarioDTO
                {
                    Id = tarefa.Criador.Id,
                    Nome = tarefa.Criador.Nome,
                    Email = tarefa.Criador.Email,
                    Telefone = tarefa.Criador.Telefone,
                    FotoPerfil = tarefa.Criador.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = tarefa.Criador.FotoPerfil.Id,
                        UrlCaminho = tarefa.Criador.FotoPerfil.UrlCaminho,
                        NomeOriginal = tarefa.Criador.FotoPerfil.NomeOriginal,
                        MimeType = tarefa.Criador.FotoPerfil.MimeType,
                        DataEnvio = tarefa.Criador.FotoPerfil.DataEnvio
                    } : null
                } : null,
                Responsaveis = tarefa.ResponsaveisTarefas.Select(rt => new ConsultaResponsavelTarefaDTO
                {
                    Id = rt.Id,
                    TarefaId = rt.TarefaId,
                    UsuarioId = rt.UsuarioId,
                    Usuario = rt.Usuario != null ? new ConsultaUsuarioDTO
                    {
                        Id = rt.Usuario.Id,
                        Nome = rt.Usuario.Nome,
                        Email = rt.Usuario.Email,
                        Telefone = rt.Usuario.Telefone,
                        FotoPerfil = rt.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                        {
                            Id = rt.Usuario.FotoPerfil.Id,
                            UrlCaminho = rt.Usuario.FotoPerfil.UrlCaminho,
                            NomeOriginal = rt.Usuario.FotoPerfil.NomeOriginal,
                            MimeType = rt.Usuario.FotoPerfil.MimeType,
                            DataEnvio = rt.Usuario.FotoPerfil.DataEnvio
                        } : null
                    } : null
                }).ToList(),
                Anexos = tarefa.Anexos.Where(a => a.RespostaTarefaId == null).Select(a => new ConsultaAnexoDTO
                {
                    Id = a.Id,
                    UrlCaminho = a.UrlCaminho,
                    NomeOriginal = a.NomeOriginal,
                    MimeType = a.MimeType,
                    DataEnvio = a.DataEnvio
                }).ToList(),
                Respostas = tarefa.Respostas.Select(r => new ConsultaRespostaTarefaDTO
                {
                    Id = r.Id,
                    DataResposta = r.DataResposta,
                    Nota = r.Nota,
                    UsuarioId = r.UsuarioId,
                    TarefaId = r.TarefaId,
                    Usuario = r.Usuario != null ? new ConsultaUsuarioDTO
                    {
                        Id = r.Usuario.Id,
                        Nome = r.Usuario.Nome,
                        Email = r.Usuario.Email,
                        Telefone = r.Usuario.Telefone,
                        FotoPerfil = r.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                        {
                            Id = r.Usuario.FotoPerfil.Id,
                            UrlCaminho = r.Usuario.FotoPerfil.UrlCaminho,
                            NomeOriginal = r.Usuario.FotoPerfil.NomeOriginal,
                            MimeType = r.Usuario.FotoPerfil.MimeType,
                            DataEnvio = r.Usuario.FotoPerfil.DataEnvio
                        } : null
                    } : null,
                    Anexos = r.Anexos.Select(ra => new ConsultaAnexoDTO
                    {
                        Id = ra.Id,
                        UrlCaminho = ra.UrlCaminho,
                        NomeOriginal = ra.NomeOriginal,
                        MimeType = ra.MimeType,
                        DataEnvio = ra.DataEnvio
                    }).ToList()
                }).ToList()
            };
        }
    }
}
