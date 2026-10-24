using Microsoft.EntityFrameworkCore;
using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class NotificacaoService : INotificacaoService
    {
        private readonly CortexDbContext _context;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEmailGateway _emailGateway;
        private readonly IPushGateway _pushGateway;
        private readonly ISmsGateway _smsGateway;
        private readonly ILogger<NotificacaoService> _logger;

        public NotificacaoService(
            CortexDbContext context,
            IUsuarioRepository usuarioRepository,
            IEmailGateway emailGateway,
            IPushGateway pushGateway,
            ISmsGateway smsGateway,
            ILogger<NotificacaoService> logger)
        {
            _context = context;
            _usuarioRepository = usuarioRepository;
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
                throw new ArgumentException("A antecedência para disparo de notificações deve ser configurada entre 1 hora e 7 dias (1 a 168 horas) (RN11).");
            }

            // RN13: Bloquear ativação por e-mail se e-mail não verificado
            if (dto.Canal == CanalNotificacao.Email && !usuario.EmailVerificado)
            {
                var algumaAtiva = dto.AtivoNovaAtribuicao || dto.AtivoVencimento || dto.AtivoConclusaoMeta || dto.AtivoTicket;
                if (algumaAtiva)
                {
                    throw new InvalidOperationException("O envio de notificações por e-mail só pode ser ativado caso o e-mail do usuário esteja verificado/confirmado (RN13).");
                }
            }

            var preferencia = await _context.PreferenciasNotificacao
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Canal == dto.Canal);

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
                    AtivoTicket = dto.AtivoTicket
                };
                _context.PreferenciasNotificacao.Add(preferencia);
            }
            else
            {
                preferencia.AntecedenciaHoras = dto.AntecedenciaHoras;
                preferencia.AtivoNovaAtribuicao = dto.AtivoNovaAtribuicao;
                preferencia.AtivoVencimento = dto.AtivoVencimento;
                preferencia.AtivoConclusaoMeta = dto.AtivoConclusaoMeta;
                preferencia.AtivoTicket = dto.AtivoTicket;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Preferência de notificação salva: UsuarioId {UsuarioId}, Canal {Canal}, Antecedência {AntecedenciaHoras}h",
                usuarioId, dto.Canal, dto.AntecedenciaHoras);

            return MapToDTO(preferencia);
        }

        public async Task<IEnumerable<PreferenciaNotificacaoDTO>> ObterPreferenciasAsync(int usuarioId)
        {
            var preferencias = await _context.PreferenciasNotificacao
                .Where(p => p.UsuarioId == usuarioId)
                .ToListAsync();

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
                        AtivoTicket = true
                    });
                }
            }

            return resultado;
        }

        public async Task<int> ProcessarAlertasVencimentoAsync()
        {
            var agoraUtc = DateTime.UtcNow;

            // Busca tarefas pendentes que ainda não venceram (RN12: descartar expiradas)
            var tarefasPendentes = await _context.Tarefas
                .AsNoTracking()
                .Include(t => t.ResponsaveisTarefas)
                .Where(t => t.Status == Status.Pendente && t.DataVencimento >= agoraUtc)
                .ToListAsync();

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
                            var assunto = $"Alerta de Vencimento: Tarefa '{tarefa.Titulo}'";
                            var mensagem = $"Olá! A tarefa '{tarefa.Titulo}' está próxima do vencimento ({tarefa.DataVencimento:dd/MM/yyyy HH:mm} UTC).";

                            var enviado = await EnviarNotificacaoAsync(usuarioId, pref.Canal, assunto, mensagem);
                            if (enviado)
                            {
                                alertasDisparados++;
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Processamento de alertas de vencimento concluído: {Count} alertas enviados", alertasDisparados);
            return alertasDisparados;
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
                AtivoTicket = p.AtivoTicket
            };
        }
    }
}
