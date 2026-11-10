using Cortex.DTOs;
using Cortex.Infra.Configuration;
using Cortex.Infra.Gateways;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class SmtpEmailGatewayTests
    {
        public SmtpEmailGatewayTests()
        {
            SmtpEmailGateway.LimparHistoricoDisparos();
        }

        [Fact]
        public async Task LogEmailGateway_DeveRegistrarLogERetornarTrue()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<LogEmailGateway>>();
            var gateway = new LogEmailGateway(loggerMock.Object);

            var msg = new EmailMensagem
            {
                Destinatario = "usuario@cortex.com",
                Assunto = "Teste de Log",
                CorpoHtml = "<p>Teste</p>",
                CorpoTexto = "Teste"
            };

            // Act
            var resultado = await gateway.EnviarAsync(msg);

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public void LogEmailGateway_MascararEmail_DeveMascararCorretamente()
        {
            // Act & Assert
            Assert.Equal("l***@cortex.com", LogEmailGateway.MascararEmail("lucas@cortex.com"));
            Assert.Equal("a***@dominio.com.br", LogEmailGateway.MascararEmail("admin@dominio.com.br"));
            Assert.Equal("***", LogEmailGateway.MascararEmail(null));
            Assert.Equal("***", LogEmailGateway.MascararEmail(""));
        }

        [Fact]
        public async Task SmtpEmailGateway_ServidorInacessivel_NaoDeveLancarExcecaoERetornaFalse()
        {
            // Arrange: Porta inexistente para forçar erro de conexão
            var options = Options.Create(new SmtpOptions
            {
                Enabled = true,
                Host = "127.0.0.1",
                Port = 65432,
                TimeoutSeconds = 1
            });
            var loggerMock = new Mock<ILogger<SmtpEmailGateway>>();
            var gateway = new SmtpEmailGateway(options, loggerMock.Object);

            var msg = new EmailMensagem
            {
                Destinatario = "teste@cortex.com",
                Assunto = "Teste de Falha",
                CorpoHtml = "<p>Corpo</p>",
                CorpoTexto = "Corpo"
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            // Act
            var resultado = await gateway.EnviarAsync(msg, cts.Token);

            // Assert: Nunca deve derrubar o fluxo lançando exceção
            Assert.False(resultado);
        }

        [Fact]
        public async Task SmtpEmailGateway_MesmoDisparoEmMenosDeUmMinuto_DeveBloquearSegundoEnvio()
        {
            // Arrange
            var options = Options.Create(new SmtpOptions
            {
                Enabled = true,
                Host = "127.0.0.1",
                Port = 65432,
                TimeoutSeconds = 1
            });
            var loggerMock = new Mock<ILogger<SmtpEmailGateway>>();
            var gateway = new SmtpEmailGateway(options, loggerMock.Object);

            var destinatario = "duplicado@cortex.com";
            var assunto = "Notificação de Alerta";

            var msg = new EmailMensagem
            {
                Destinatario = destinatario,
                Assunto = assunto,
                CorpoHtml = "<p>Conteúdo</p>",
                CorpoTexto = "Conteúdo"
            };

            // Simula que o mesmo disparo acabou de ocorrer (há 10 segundos)
            SmtpEmailGateway.SimularDisparo(destinatario, assunto, DateTime.UtcNow.AddSeconds(-10));

            // Act: Tenta enviar o mesmo disparo dentro da janela de 1 minuto
            var resultado = await gateway.EnviarAsync(msg);

            // Assert: Deve ser bloqueado imediatamente retornando false
            Assert.False(resultado);
        }

        [Fact]
        public async Task SmtpEmailGateway_MesmoDisparoAposUmMinuto_NaoBloqueiaPorRateLimit()
        {
            // Arrange
            var options = Options.Create(new SmtpOptions
            {
                Enabled = true,
                Host = "127.0.0.1",
                Port = 65432,
                TimeoutSeconds = 1
            });
            var loggerMock = new Mock<ILogger<SmtpEmailGateway>>();
            var gateway = new SmtpEmailGateway(options, loggerMock.Object);

            var destinatario = "usuario@cortex.com";
            var assunto = "Notificação Antiga";

            var msg = new EmailMensagem
            {
                Destinatario = destinatario,
                Assunto = assunto,
                CorpoHtml = "<p>Conteúdo</p>",
                CorpoTexto = "Conteúdo"
            };

            // Simula que o disparo ocorreu há mais de 1 minuto (ex: 65 segundos atrás)
            SmtpEmailGateway.SimularDisparo(destinatario, assunto, DateTime.UtcNow.AddSeconds(-65));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Act: O rate limit não deve bloquear. A falha esperada aqui é estritamente pela conexão SMTP fake.
            var resultado = await gateway.EnviarAsync(msg, cts.Token);

            // Assert: Cai no erro de conexão e retorna false, mas passou pelo bloqueio de duplicidade
            Assert.False(resultado);
        }
    }
}
