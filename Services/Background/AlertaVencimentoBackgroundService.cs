using Cortex.Services.Interfaces;

namespace Cortex.Services.Background
{
    public class AlertaVencimentoBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertaVencimentoBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(1);

        public AlertaVencimentoBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<AlertaVencimentoBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlertaVencimentoBackgroundService iniciado. Intervalo de verificação: {Intervalo}", _intervalo);

            try
            {
                // Executa na inicialização se o cancelamento não foi solicitado
                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessarAlertasAsync(stoppingToken);
                }

                using var timer = new PeriodicTimer(_intervalo);

                while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ProcessarAlertasAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown gracioso
            }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
            {
                // Provedor de serviços descartado durante o shutdown
            }
            catch (Exception ex)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Erro fatal no AlertaVencimentoBackgroundService.");
                }
            }
        }

        private async Task ProcessarAlertasAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificacaoService = scope.ServiceProvider.GetRequiredService<INotificacaoService>();

                var totalAlertas = await notificacaoService.ProcessarAlertasVencimentoAsync();

                if (totalAlertas > 0)
                {
                    _logger.LogInformation("Verificação automática de vencimentos: {Total} alerta(s) disparado(s).", totalAlertas);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Encerramento normal
            }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
            {
                // Encerramento do container
            }
            catch (Exception ex)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Erro durante execução periódica do AlertaVencimentoBackgroundService.");
                }
            }
        }
    }
}
