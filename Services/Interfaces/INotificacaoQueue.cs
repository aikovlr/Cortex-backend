using Cortex.DTOs;

namespace Cortex.Services.Interfaces
{
    public interface INotificacaoQueue
    {
        ValueTask EnfileirarAsync(NotificacaoDespacho item, CancellationToken ct = default);

        IAsyncEnumerable<NotificacaoDespacho> LerTodosAsync(CancellationToken ct = default);
    }
}

