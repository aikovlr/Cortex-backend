using System.Threading.Channels;
using Cortex.DTOs;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class InMemoryNotificacaoQueue : INotificacaoQueue
    {
        private readonly Channel<NotificacaoDespacho> _channel;

        public InMemoryNotificacaoQueue()
        {
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateUnbounded<NotificacaoDespacho>(options);
        }

        public ValueTask EnfileirarAsync(NotificacaoDespacho item, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            return _channel.Writer.WriteAsync(item, ct);
        }

        public IAsyncEnumerable<NotificacaoDespacho> LerTodosAsync(CancellationToken ct = default)
        {
            return _channel.Reader.ReadAllAsync(ct);
        }
    }
}

