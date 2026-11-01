using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class NotificacaoService : INotificacaoService
    {
        private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IAlertaEnviadoRepository _alertaEnviadoRepository;
        private readonly INotificacaoRepository _notificacaoRepository;
        private readonly IEmailGateway _emailGateway;
        private readonly IPushGateway _pushGateway;
        private readonly ISmsGateway _smsGateway;
        private readonly ILogger<NotificacaoService> _logger;

        public NotificacaoService(
            IPreferenciaNotificacaoRepository preferenciaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IAlertaEnviadoRepository alertaEnviadoRepository,
            INotificacaoRepository notificacaoRepository,
            IEmailGateway emailGateway,
            IPushGateway pushGateway,
            ISmsGateway smsGateway,
            ILogger<NotificacaoService> logger)
        {
            _preferenciaRepository = preferenciaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _alertaEnviadoRepository = alertaEnviadoRepository;
            _notificacaoRepository = notificacaoRepository;
            _emailGateway = emailGateway;
            _pushGateway = pushGateway;
            _smsGateway = smsGateway;
            _logger = logger;
        }

        public async Task<bool> EnviarNotificacaoAsync(int usuarioId, CanalNotificacao canal, string assunto, string mensagem)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
            if (usuario == null)
            {
                _logger.LogWarning("Tentativa de envio de notificação para usuário inexistente: {UsuarioId}", usuarioId);
                return false;
            }

            switch (canal)
            {
                case CanalNotificacao.Email:
                    // RN13: O envio de notificações por e-mail só pode ser ativado caso o e-mail do usuário esteja verificado/confirmado
                    if (!usuario.EmailVerificado)
                    {
                        _logger.LogWarning("Envio de e-mail bloqueado para UsuarioId {UsuarioId}: e-mail não confirmado (RN13).", usuarioId);
                        return false;
                    }
                    return await _emailGateway.EnviarEmailAsync(usuario.Email, assunto, mensagem);

                case CanalNotificacao.Push:
                    return await _pushGateway.EnviarPushAsync(usuarioId, assunto, mensagem);

                case CanalNotificacao.Sms:
                    if (string.IsNullOrWhiteSpace(usuario.Telefone))
                    {
                        _logger.LogWarning("Envio de SMS ignorado para UsuarioId {UsuarioId}: telefone não cadastrado.", usuarioId);
                        return false;
                    }
                    return await _smsGateway.EnviarSmsAsync(usuario.Telefone, mensagem);

                default:
                    return false;
            }
        }

        public async Task<PreferenciaNotificacaoDTO> ConfigurarPreferenciasAsync(int usuarioId, ConfigurarPreferenciaDTO dto)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            // RN11: Janela de antecedência 1h a 168h (7 dias)
            if (dto.AntecedenciaHoras < 1 || dto.AntecedenciaHoras > 168)
            {
                throw new ArgumentException("A antecedência para disparo de notificações deve ser configurada entre 1 hora e 7 dias (1 a 168 horas).");
            }

            // RN13: Bloquear ativação por e-mail se e-mail não verificado
            if (dto.Canal == CanalNotificacao.Email && !usuario.EmailVerificado)
            {
                var algumaAtiva = dto.AtivoNovaAtribuicao || dto.AtivoVencimento || dto.AtivoConclusaoMeta || dto.AtivoTicket;
                if (algumaAtiva)
                {
                    throw new InvalidOperationException("O envio de notificações por e-mail só pode ser ativado caso o e-mail do usuário esteja verificado/confirmado.");
                }
            }

            var preferencia = await _preferenciaRepository.ObterPorUsuarioECanalAsync(usuarioId, dto.Canal);

            if (preferencia == null)
            {
                preferencia = new PreferenciaNotificacao
                {
                    UsuarioId = usuarioId,
                    Canal = dto.Canal,
                    AntecedenciaHoras = dto.AntecedenciaHoras,
                    AtivoNovaAtribuicao = dto.AtivoNovaAtribuicao,
                    AtivoVencimento = dto.AtivoVencimento,
                    AtivoConclusaoMeta = dto.AtivoConclusaoMeta,
                    AtivoTicket = dto.AtivoTicket,
                    Tema = dto.Tema ?? "light",
                    AltoContraste = dto.AltoContraste ?? false,
                    TamanhoFonte = dto.TamanhoFonte ?? 16
                };
                await _preferenciaRepository.AdicionarAsync(preferencia);
            }
            else
            {
                preferencia.AntecedenciaHoras = dto.AntecedenciaHoras;
                preferencia.AtivoNovaAtribuicao = dto.AtivoNovaAtribuicao;
                preferencia.AtivoVencimento = dto.AtivoVencimento;
                preferencia.AtivoConclusaoMeta = dto.AtivoConclusaoMeta;
                preferencia.AtivoTicket = dto.AtivoTicket;
                if (dto.Tema != null) preferencia.Tema = dto.Tema;
                if (dto.AltoContraste.HasValue) preferencia.AltoContraste = dto.AltoContraste.Value;
                if (dto.TamanhoFonte.HasValue) preferencia.TamanhoFonte = dto.TamanhoFonte.Value;
                await _preferenciaRepository.AtualizarAsync(preferencia);
            }

            _logger.LogInformation("Preferência de notificação salva: UsuarioId {UsuarioId}, Canal {Canal}, Antecedência {AntecedenciaHoras}h",
                usuarioId, dto.Canal, dto.AntecedenciaHoras);

            return MapToDTO(preferencia);
        }

        public async Task<IEnumerable<PreferenciaNotificacaoDTO>> ObterPreferenciasAsync(int usuarioId)
        {
            var preferencias = await _preferenciaRepository.ObterPorUsuarioAsync(usuarioId);

            var resultado = new List<PreferenciaNotificacaoDTO>();
            var canais = Enum.GetValues<CanalNotificacao>();

            foreach (var canal in canais)
            {
                var pref = preferencias.FirstOrDefault(p => p.Canal == canal);
                if (pref != null)
                {
                    resultado.Add(MapToDTO(pref));
                }
                else
                {
                    // Padrão do sistema
                    resultado.Add(new PreferenciaNotificacaoDTO
                    {
                        Id = 0,
                        UsuarioId = usuarioId,
                        Canal = canal,
                        AntecedenciaHoras = 24,
                        AtivoNovaAtribuicao = true,
                        AtivoVencimento = true,
                        AtivoConclusaoMeta = true,
                        AtivoTicket = true,
                        Tema = "light",
                        AltoContraste = false,
                        TamanhoFonte = 16
                    });
                }
            }

            return resultado;
        }

        public async Task<PreferenciaAcessibilidadeDTO> ObterPreferenciaAcessibilidadeAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            var preferencias = await _preferenciaRepository.ObterPorUsuarioAsync(usuarioId);
            var pref = preferencias.FirstOrDefault();

            if (pref != null)
            {
                return new PreferenciaAcessibilidadeDTO
                {
                    Tema = pref.Tema ?? "light",
                    AltoContraste = pref.AltoContraste,
                    TamanhoFonte = pref.TamanhoFonte
                };
            }

            return new PreferenciaAcessibilidadeDTO
            {
                Tema = "light",
                AltoContraste = false,
                TamanhoFonte = 16
            };
        }

        public async Task<PreferenciaAcessibilidadeDTO> AtualizarPreferenciaAcessibilidadeAsync(int usuarioId, AtualizarPreferenciaAcessibilidadeDTO dto)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            if (dto.Tema != null && !dto.Tema.Equals("light", StringComparison.OrdinalIgnoreCase) && !dto.Tema.Equals("dark", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("O tema selecionado deve ser 'light' ou 'dark'.");
            }

            if (dto.TamanhoFonte.HasValue)
            {
                var fonte = dto.TamanhoFonte.Value;
                if (fonte < 12 || fonte > 24 || fonte % 2 != 0)
                {
                    throw new ArgumentException("O tamanho da fonte deve estar entre 12px e 24px e ser obrigatoriamente um número par (12, 14, 16, 18, 20, 22 ou 24).");
                }
            }

            await _preferenciaRepository.AtualizarAcessibilidadeUsuarioAsync(
                usuarioId,
                dto.Tema?.ToLowerInvariant(),
                dto.AltoContraste,
                dto.TamanhoFonte
            );

            _logger.LogInformation("Preferências de acessibilidade atualizadas para o UsuarioId {UsuarioId}: Tema={Tema}, AltoContraste={AltoContraste}, TamanhoFonte={TamanhoFonte}",
                usuarioId, dto.Tema, dto.AltoContraste, dto.TamanhoFonte);

            return await ObterPreferenciaAcessibilidadeAsync(usuarioId);
        }

        public async Task<int> ProcessarAlertasVencimentoAsync()
        {
            var agoraUtc = DateTime.UtcNow;

            // Busca tarefas pendentes que ainda não venceram (RN12: descartar expiradas)
            var tarefasPendentes = await _tarefaRepository.ObterTarefasPendentesAVencerAsync(agoraUtc);

            var alertasDisparados = 0;

            foreach (var tarefa in tarefasPendentes)
            {
                var responsaveisIds = tarefa.ResponsaveisTarefas.Select(r => r.UsuarioId).Distinct().ToList();

                foreach (var usuarioId in responsaveisIds)
                {
                    var preferencias = await ObterPreferenciasAsync(usuarioId);

                    foreach (var pref in preferencias.Where(p => p.AtivoVencimento))
                    {
                        var janelaAlerta = agoraUtc.AddHours(pref.AntecedenciaHoras);

                        // Se a data de vencimento está dentro da janela de antecedência configurada
                        if (tarefa.DataVencimento <= janelaAlerta)
                        {
                            var jaEnviado = await _alertaEnviadoRepository
                                .ExisteAlertaEnviadoAsync(usuarioId, tarefa.Id, pref.Canal, agoraUtc.AddHours(-pref.AntecedenciaHoras));

                            if (!jaEnviado)
                            {
                                var assunto = $"Alerta de Vencimento: Tarefa '{tarefa.Titulo}'";
                                var mensagem = $"Olá! A tarefa '{tarefa.Titulo}' está próxima do vencimento ({tarefa.DataVencimento:dd/MM/yyyy HH:mm} UTC).";

                                var enviado = await EnviarNotificacaoAsync(usuarioId, pref.Canal, assunto, mensagem);
                                if (enviado)
                                {
                                    await _alertaEnviadoRepository.RegistrarAsync(usuarioId, tarefa.Id, pref.Canal);
                                    alertasDisparados++;
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Processamento de alertas de vencimento concluído: {Count} alertas enviados", alertasDisparados);
            return alertasDisparados;
        }

        // ==========================================
        // Notificações persistidas no Banco de Dados
        // ==========================================

        public async Task<NotificacaoDTO> CriarNotificacaoAsync(
            int usuarioId,
            string tipo,
            string titulo,
            string mensagem,
            string link,
            bool destaque,
            int? tarefaId = null)
        {
            // Quando for notificação de conclusão de tarefa, stacka na notificação existente da mesma tarefa para não poluir
            if (tarefaId.HasValue && titulo.StartsWith("Tarefa Concluída"))
            {
                var existente = await _notificacaoRepository
                    .ObterNotificacaoPorUsuarioETarefaAsync(usuarioId, tarefaId.Value, tipo);

                if (existente != null && existente.Titulo.StartsWith("Tarefa Concluída"))
                {
                    existente.Titulo = titulo;
                    existente.Mensagem = mensagem;
                    existente.Link = link;
                    existente.Destaque = destaque;
                    existente.DataCriacao = DateTime.UtcNow;
                    existente.Lida = false;

                    await _notificacaoRepository.AtualizarAsync(existente);

                    _logger.LogInformation("Notificação de conclusão stackada: Id {Id}, UsuarioId {UsuarioId}, TarefaId {TarefaId}",
                        existente.Id, usuarioId, tarefaId.Value);

                    return MapNotificacaoToDTO(existente);
                }
            }

            var notificacao = new Notificacao
            {
                UsuarioId = usuarioId,
                Tipo = tipo,
                Titulo = titulo,
                Mensagem = mensagem,
                Link = link,
                Destaque = destaque,
                TarefaId = tarefaId,
                Lida = false,
                DataCriacao = DateTime.UtcNow
            };

            await _notificacaoRepository.AdicionarAsync(notificacao);

            _logger.LogInformation("Notificação persistida no banco: Id {Id}, UsuarioId {UsuarioId}, Tipo {Tipo}",
                notificacao.Id, usuarioId, tipo);

            return MapNotificacaoToDTO(notificacao);
        }

        public async Task<IEnumerable<NotificacaoDTO>> ObterNotificacoesUsuarioAsync(int usuarioId)
        {
            var notificacoes = await _notificacaoRepository.ObterPorUsuarioAsync(usuarioId);
            return notificacoes.Select(MapNotificacaoToDTO);
        }

        public async Task<bool> MarcarComoLidaAsync(int notificacaoId, int usuarioId)
        {
            var notif = await _notificacaoRepository.ObterPorIdAsync(notificacaoId);
            if (notif == null || notif.UsuarioId != usuarioId)
                return false;

            notif.Lida = true;
            await _notificacaoRepository.AtualizarAsync(notif);
            return true;
        }

        public async Task<bool> MarcarTodasComoLidasAsync(int usuarioId)
        {
            await _notificacaoRepository.MarcarTodasComoLidasAsync(usuarioId);
            return true;
        }

        public async Task<bool> LimparNotificacoesAsync(int usuarioId)
        {
            await _notificacaoRepository.LimparNotificacoesAsync(usuarioId);
            return true;
        }

        public async Task<bool> RemoverNotificacaoAsync(int notificacaoId, int usuarioId)
        {
            return await _notificacaoRepository.RemoverNotificacaoAsync(notificacaoId, usuarioId);
        }

        private static NotificacaoDTO MapNotificacaoToDTO(Notificacao n)
        {
            return new NotificacaoDTO
            {
                Id = n.Id,
                UsuarioId = n.UsuarioId,
                Tipo = n.Tipo,
                Titulo = n.Titulo,
                Mensagem = n.Mensagem,
                Link = n.Link,
                Lida = n.Lida,
                Destaque = n.Destaque,
                TarefaId = n.TarefaId,
                DataCriacao = n.DataCriacao
            };
        }

        private static PreferenciaNotificacaoDTO MapToDTO(PreferenciaNotificacao p)
        {
            return new PreferenciaNotificacaoDTO
            {
                Id = p.Id,
                UsuarioId = p.UsuarioId,
                Canal = p.Canal,
                AntecedenciaHoras = p.AntecedenciaHoras,
                AtivoNovaAtribuicao = p.AtivoNovaAtribuicao,
                AtivoVencimento = p.AtivoVencimento,
                AtivoConclusaoMeta = p.AtivoConclusaoMeta,
                AtivoTicket = p.AtivoTicket,
                Tema = p.Tema,
                AltoContraste = p.AltoContraste,
                TamanhoFonte = p.TamanhoFonte
            };
        }
    }
}
