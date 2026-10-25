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
            var alertaEnviadoRepo = new AlertaEnviadoRepository(_dbContext);

            _notificacaoService = new NotificacaoService(
                preferenciaRepo,
                tarefaRepo,
                _usuarioRepoMock.Object,
                alertaEnviadoRepo,
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

        [Fact]
        public async Task ProcessarAlertasVencimento_NaoDeveEnviarAlertaDuplicadoNaMesmaJanela()
        {
            // Arrange
            var criador = new Usuario
            {
                Id = 10,
                Nome = "Criador",
                Email = "criador@teste.com",
                SenhaHash = "hash",
                EmailVerificado = true
            };
            var usuario = new Usuario
            {
                Id = 11,
                Nome = "Responsavel",
                Email = "resp@teste.com",
                SenhaHash = "hash",
                EmailVerificado = true
            };
            await _dbContext.Usuarios.AddRangeAsync(criador, usuario);

            var tarefa = new Tarefa
            {
                Id = 100,
                Titulo = "Tarefa Urgente",
                DataVencimento = DateTime.UtcNow.AddHours(5),
                Status = Status.Pendente,
                Prioridade = Prioridade.Alta,
                CriadorId = criador.Id,
                Criador = criador
            };
            await _dbContext.Tarefas.AddAsync(tarefa);

            var resp = new ResponsavelTarefa
            {
                TarefaId = tarefa.Id,
                UsuarioId = usuario.Id
            };
            await _dbContext.ResponsaveisTarefa.AddAsync(resp);

            var pref = new PreferenciaNotificacao
            {
                UsuarioId = usuario.Id,
                Canal = CanalNotificacao.Push,
                AntecedenciaHoras = 24,
                AtivoVencimento = true
            };
            await _dbContext.PreferenciasNotificacao.AddAsync(pref);
            await _dbContext.SaveChangesAsync();

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(usuario.Id)).ReturnsAsync(usuario);
            _pushGatewayMock.Setup(p => p.EnviarPushAsync(usuario.Id, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act 1: primeira execução - deve disparar 1 alerta
            var disparos1 = await _notificacaoService.ProcessarAlertasVencimentoAsync();

            // Act 2: segunda execução imediata - NÃO deve duplicar o alerta
            var disparos2 = await _notificacaoService.ProcessarAlertasVencimentoAsync();

            // Assert
            Assert.Equal(1, disparos1);
            Assert.Equal(0, disparos2);
            _pushGatewayMock.Verify(p => p.EnviarPushAsync(usuario.Id, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
