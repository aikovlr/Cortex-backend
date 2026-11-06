using Cortex.DTOs;
using Cortex.Entities;
using Cortex.Services.Implementations;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class NotificacaoQueueTests
    {
        [Fact]
        public async Task InMemoryNotificacaoQueue_DeveEnfileirarELerItensComSucesso()
        {
            // Arrange
            var queue = new InMemoryNotificacaoQueue();
            var item = new NotificacaoDespacho
            {
                Canal = CanalNotificacao.Email,
                UsuarioId = 10,
                Destinatario = "teste@cortex.com",
                Assunto = "Assunto Teste",
                MensagemTexto = "Mensagem",
                TipoEvento = "tarefa"
            };

            // Act
            await queue.EnfileirarAsync(item);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            NotificacaoDespacho? recebido = null;

            await foreach (var desp in queue.LerTodosAsync(cts.Token))
            {
                recebido = desp;
                break;
            }

            // Assert
            Assert.NotNull(recebido);
            Assert.Equal("teste@cortex.com", recebido.Destinatario);
            Assert.Equal(CanalNotificacao.Email, recebido.Canal);
            Assert.Equal("Assunto Teste", recebido.Assunto);
        }
    }
}

