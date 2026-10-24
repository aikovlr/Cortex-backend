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

            // Executa imediatamente na inicialização
            await VerificarTarefasAsync(stoppingToken);

            using var timer = new PeriodicTimer(_intervalo);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await VerificarTarefasAsync(stoppingToken);
            }
        }

        private async Task VerificarTarefasAsync(CancellationToken stoppingToken)
        {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante execução periódica do TarefaStatusBackgroundService.");
            }
        }
    }
}
