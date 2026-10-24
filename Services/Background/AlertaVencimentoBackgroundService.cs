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

            // Executa imediatamente na inicialização
            await ProcessarAlertasAsync(stoppingToken);

            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessarAlertasAsync(stoppingToken);
            }
        }

        private async Task ProcessarAlertasAsync(CancellationToken stoppingToken)
        {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante execução periódica do AlertaVencimentoBackgroundService.");
            }
        }
    }
}
