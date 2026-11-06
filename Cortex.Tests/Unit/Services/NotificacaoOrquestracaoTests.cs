using Cortex.DTOs;
using Cortex.Entities;
using Cortex.Entities.ENUMS;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Implementations;
using Cortex.Repositories.Interface;
using Cortex.Services.Implementations;
using Cortex.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class NotificacaoOrquestracaoTests
    {
        private readonly CortexDbContext _dbContext;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEmailGateway> _emailGatewayMock;
        private readonly Mock<IPushGateway> _pushGatewayMock;
        private readonly Mock<ISmsGateway> _smsGatewayMock;
        private readonly Mock<INotificacaoQueue> _queueMock;
        private readonly Mock<IEmailTemplateService> _templateMock;
        private readonly Mock<ILogger<NotificacaoService>> _loggerMock;
        private readonly NotificacaoService _notificacaoService;

        public NotificacaoOrquestracaoTests()
        {
            var options = new DbContextOptionsBuilder<CortexDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CortexDbContext(options);

            var preferenciaRepo = new PreferenciaNotificacaoRepository(_dbContext);
            var tarefaRepo = new TarefaRepository(_dbContext);
            var alertaEnviadoRepo = new AlertaEnviadoRepository(_dbContext);
            var notificacaoRepo = new NotificacaoRepository(_dbContext);

            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _emailGatewayMock = new Mock<IEmailGateway>();
            _pushGatewayMock = new Mock<IPushGateway>();
            _smsGatewayMock = new Mock<ISmsGateway>();
            _queueMock = new Mock<INotificacaoQueue>();
            _templateMock = new Mock<IEmailTemplateService>();
            _loggerMock = new Mock<ILogger<NotificacaoService>>();

            _notificacaoService = new NotificacaoService(
                preferenciaRepo,
                tarefaRepo,
                _usuarioRepoMock.Object,
                alertaEnviadoRepo,
                notificacaoRepo,
                _emailGatewayMock.Object,
                _pushGatewayMock.Object,
                _smsGatewayMock.Object,
                _queueMock.Object,
                _templateMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task NotificarAsync_RN12_NaoEnviaEmailSeNaoVerificado_MasGeraInApp()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 10,
                Nome = "Lucas Teste",
                Email = "lucas@cortex.com",
                SenhaHash = "hash",
                EmailVerificado = false // NÃO verificado
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(user.Id)).ReturnsAsync(user);

            // Act
            var notif = await _notificacaoService.NotificarAsync(
                TipoEventoNotificacao.NovaAtribuicao,
                user.Id,
                "Nova Tarefa",
                "Você recebeu uma tarefa.",
                "/tarefas/1");

            // Assert: Notificação in-app criada
            Assert.NotNull(notif);
            Assert.Equal("Nova Tarefa", notif.Titulo);
            Assert.False(notif.Lida);

            // Assert: Email NÃO enfileirado para o usuário não verificado
            _queueMock.Verify(q => q.EnfileirarAsync(
                It.Is<NotificacaoDespacho>(d => d.Canal == CanalNotificacao.Email),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task NotificarAsync_EmailVerificadoEPreferenciaAtiva_DeveEnfileirarDespacho()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 11,
                Nome = "Beatriz Verificada",
                Email = "beatriz@cortex.com",
                SenhaHash = "hash",
                EmailVerificado = true // Verificado
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(user.Id)).ReturnsAsync(user);

            _templateMock.Setup(t => t.GerarNotificacaoEvento(
                user.Email, user.Nome, "tarefa", "Nova Atribuição", "Mensagem", "/tarefas/2"))
                .Returns(new EmailMensagem
                {
                    Destinatario = user.Email,
                    Assunto = "Nova Atribuição — Cortex",
                    CorpoHtml = "<p>Html</p>",
                    CorpoTexto = "Texto"
                });

            // Act
            var notif = await _notificacaoService.NotificarAsync(
                TipoEventoNotificacao.NovaAtribuicao,
                user.Id,
                "Nova Atribuição",
                "Mensagem",
                "/tarefas/2");

            // Assert
            Assert.NotNull(notif);
            _queueMock.Verify(q => q.EnfileirarAsync(
                It.Is<NotificacaoDespacho>(d => d.Canal == CanalNotificacao.Email && d.Destinatario == user.Email),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NotificarAsync_RespeitaPreferenciaDesativada_NaoDeveEnfileirar()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 12,
                Nome = "Marcos",
                Email = "marcos@cortex.com",
                SenhaHash = "hash",
                EmailVerificado = true
            };

            // Preferência desativando nova atribuição no canal e-mail
            var pref = new PreferenciaNotificacao
            {
                UsuarioId = user.Id,
                Canal = CanalNotificacao.Email,
                AtivoNovaAtribuicao = false // Desativado!
            };
            await _dbContext.PreferenciasNotificacao.AddAsync(pref);
            await _dbContext.SaveChangesAsync();

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(user.Id)).ReturnsAsync(user);

            // Act
            await _notificacaoService.NotificarAsync(
                TipoEventoNotificacao.NovaAtribuicao,
                user.Id,
                "Tarefa X",
                "Mensagem");

            // Assert: Não deve ter enfileirado para e-mail
            _queueMock.Verify(q => q.EnfileirarAsync(
                It.Is<NotificacaoDespacho>(d => d.Canal == CanalNotificacao.Email),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

