using System.Diagnostics;
using Cortex.DTOs;
using Cortex.Entities;
using Cortex.Infra.Gateways;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Background
{
    public class NotificacaoDispatcherBackgroundService : BackgroundService
    {
        private readonly INotificacaoQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacaoDispatcherBackgroundService> _logger;

        public NotificacaoDispatcherBackgroundService(
            INotificacaoQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificacaoDispatcherBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificacaoDispatcherBackgroundService iniciado. Aguardando itens na fila...");

            try
            {
                await foreach (var despacho in _queue.LerTodosAsync(stoppingToken))
                {
                    await ProcessarDespachoAsync(despacho, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("NotificacaoDispatcherBackgroundService encerrando de forma graciosa (shutdown solicitado).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal não tratado no NotificacaoDispatcherBackgroundService.");
            }
        }

        private async Task ProcessarDespachoAsync(NotificacaoDespacho despacho, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var emailMascarado = LogEmailGateway.MascararEmail(despacho.Destinatario);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sucesso = false;

                switch (despacho.Canal)
                {
                    case CanalNotificacao.Email:
                        var emailGateway = scope.ServiceProvider.GetRequiredService<IEmailGateway>();
                        var emailMensagem = new EmailMensagem
                        {
                            Destinatario = despacho.Destinatario,
                            NomeDestinatario = despacho.NomeDestinatario,
                            Assunto = despacho.Assunto,
                            CorpoHtml = despacho.MensagemHtml ?? $"<p>{despacho.MensagemTexto}</p>",
                            CorpoTexto = despacho.MensagemTexto
                        };
                        sucesso = await emailGateway.EnviarAsync(emailMensagem, ct);
                        break;

                    case CanalNotificacao.Push:
                        var pushGateway = scope.ServiceProvider.GetRequiredService<IPushGateway>();
                        sucesso = await pushGateway.EnviarPushAsync(despacho.UsuarioId, despacho.Assunto, despacho.MensagemTexto);
                        break;

                    case CanalNotificacao.Sms:
                        var smsGateway = scope.ServiceProvider.GetRequiredService<ISmsGateway>();
                        sucesso = await smsGateway.EnviarSmsAsync(despacho.Destinatario, despacho.MensagemTexto);
                        break;
                }

                sw.Stop();

                if (sucesso)
                {
                    _logger.LogInformation(
                        "[DISPATCHER] Despacho concluído com sucesso | Canal: {Canal} | Evento: {TipoEvento} | UsuarioId: {UsuarioId} | TarefaId: {TarefaId} | Destinatário: {Destinatario} | Duração: {ElapsedMilliseconds}ms",
                        despacho.Canal,
                        despacho.TipoEvento,
                        despacho.UsuarioId,
                        despacho.TarefaId,
                        emailMascarado,
                        sw.ElapsedMilliseconds);

                    // Auditoria em AlertaEnviado se for notificação vinculada a tarefa
                    if (despacho.TarefaId.HasValue)
                    {
                        try
                        {
                            var alertaRepository = scope.ServiceProvider.GetRequiredService<IAlertaEnviadoRepository>();
                            await alertaRepository.RegistrarAsync(despacho.UsuarioId, despacho.TarefaId.Value, despacho.Canal);
                        }
                        catch (Exception exAlerta)
                        {
                            _logger.LogWarning(exAlerta, "Erro ao registrar AlertaEnviado para UsuarioId {UsuarioId}, TarefaId {TarefaId}",
                                despacho.UsuarioId, despacho.TarefaId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[DISPATCHER] Despacho falhou ou foi rejeitado | Canal: {Canal} | Evento: {TipoEvento} | UsuarioId: {UsuarioId} | TarefaId: {TarefaId} | Destinatário: {Destinatario} | Duração: {ElapsedMilliseconds}ms",
                        despacho.Canal,
                        despacho.TipoEvento,
                        despacho.UsuarioId,
                        despacho.TarefaId,
                        emailMascarado,
                        sw.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("[DISPATCHER] Processamento do despacho cancelado devido ao encerramento do serviço.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    ex,
                    "[DISPATCHER] Exceção ao processar despacho | Canal: {Canal} | Evento: {TipoEvento} | UsuarioId: {UsuarioId} | TarefaId: {TarefaId} | Duração: {ElapsedMilliseconds}ms",
                    despacho.Canal,
                    despacho.TipoEvento,
                    despacho.UsuarioId,
                    despacho.TarefaId,
                    sw.ElapsedMilliseconds);
            }
        }
    }
}

