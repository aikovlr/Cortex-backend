using Cortex.Services.Interfaces;

namespace Cortex.Services.Background
{
    public class TarefaStatusBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TarefaStatusBackgroundService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

        public TarefaStatusBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TarefaStatusBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TarefaStatusBackgroundService iniciado. Intervalo de verificação: {Intervalo}", _intervalo);

            try
            {
                // Executa na inicialização se o cancelamento não foi solicitado
                if (!stoppingToken.IsCancellationRequested)
                {
                    await VerificarTarefasAsync(stoppingToken);
                }

                using var timer = new PeriodicTimer(_intervalo);

                while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await VerificarTarefasAsync(stoppingToken);
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
                    _logger.LogError(ex, "Erro fatal no TarefaStatusBackgroundService.");
                }
            }
        }

        private async Task VerificarTarefasAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var tarefaService = scope.ServiceProvider.GetRequiredService<ITarefaService>();

                var totalAtrasadas = await tarefaService.VerificarTarefasAtrasadasAsync();

                if (totalAtrasadas > 0)
                {
                    _logger.LogInformation("Verificação automática: {Total} tarefa(s) transicionada(s) para status Atrasada.", totalAtrasadas);
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
                    _logger.LogError(ex, "Erro durante execução periódica do TarefaStatusBackgroundService.");
                }
            }
        }
    }
}
