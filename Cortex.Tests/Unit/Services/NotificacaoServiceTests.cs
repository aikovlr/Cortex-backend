using Cortex.DTOs.Request;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Cortex.Repositories.Implementations;
using Cortex.Services.Implementations;
using Cortex.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class NotificacaoServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEmailGateway> _emailGatewayMock;
        private readonly Mock<IPushGateway> _pushGatewayMock;
        private readonly Mock<ISmsGateway> _smsGatewayMock;
        private readonly Mock<ILogger<NotificacaoService>> _loggerMock;
        private readonly CortexDbContext _dbContext;
        private readonly NotificacaoService _notificacaoService;

        public NotificacaoServiceTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _emailGatewayMock = new Mock<IEmailGateway>();
            _pushGatewayMock = new Mock<IPushGateway>();
            _smsGatewayMock = new Mock<ISmsGateway>();
            _loggerMock = new Mock<ILogger<NotificacaoService>>();

            var options = new DbContextOptionsBuilder<CortexDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CortexDbContext(options);

            var preferenciaRepo = new PreferenciaNotificacaoRepository(_dbContext);
            var tarefaRepo = new TarefaRepository(_dbContext);

            _notificacaoService = new NotificacaoService(
                preferenciaRepo,
                tarefaRepo,
                _usuarioRepoMock.Object,
                _emailGatewayMock.Object,
                _pushGatewayMock.Object,
                _smsGatewayMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ConfigurarPreferencia_RN13_DeveBloquearEmailSeEmailNaoVerificado()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Usuario Teste",
                Email = "teste@exemplo.com",
                SenhaHash = "hash",
                EmailVerificado = false // Não verificado
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(usuario.Id)).ReturnsAsync(usuario);

            var dto = new ConfigurarPreferenciaDTO
            {
                Canal = CanalNotificacao.Email,
                AntecedenciaHoras = 24,
                AtivoVencimento = true
            };

            // Act & Assert (RN13)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _notificacaoService.ConfigurarPreferenciasAsync(usuario.Id, dto));

            Assert.Contains("RN13", ex.Message);
        }

        [Fact]
        public async Task ConfigurarPreferencia_RN13_DevePermitirEmailSeEmailVerificado()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 2,
                Nome = "Usuario Verificado",
                Email = "verificado@exemplo.com",
                SenhaHash = "hash",
                EmailVerificado = true // Verificado
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(usuario.Id)).ReturnsAsync(usuario);

            var dto = new ConfigurarPreferenciaDTO
            {
                Canal = CanalNotificacao.Email,
                AntecedenciaHoras = 12,
                AtivoVencimento = true
            };

            // Act
            var resultado = await _notificacaoService.ConfigurarPreferenciasAsync(usuario.Id, dto);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(CanalNotificacao.Email, resultado.Canal);
            Assert.True(resultado.AtivoVencimento);
            Assert.Equal(12, resultado.AntecedenciaHoras);
        }
    }
}
