using Cortex.DTOs.Request;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Implementations;
using Cortex.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class MembroEquipeServiceTests
    {
        private readonly Mock<IMembroEquipeRepository> _membroEquipeRepoMock;
        private readonly Mock<IEquipeRepository> _equipeRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<INotificacaoService> _notificacaoServiceMock;
        private readonly Mock<ILogger<MembroEquipeService>> _loggerMock;
        private readonly MembroEquipeService _membroEquipeService;

        public MembroEquipeServiceTests()
        {
            _membroEquipeRepoMock = new Mock<IMembroEquipeRepository>();
            _equipeRepoMock = new Mock<IEquipeRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _notificacaoServiceMock = new Mock<INotificacaoService>();
            _loggerMock = new Mock<ILogger<MembroEquipeService>>();

            _membroEquipeService = new MembroEquipeService(
                _membroEquipeRepoMock.Object,
                _equipeRepoMock.Object,
                _usuarioRepoMock.Object,
                _loggerMock.Object,
                _notificacaoServiceMock.Object);
        }

        [Fact]
        public async Task AdicionarMembro_ComSucesso_DeveCriarNotificacaoParaOUsuarioAdicionado()
        {
            // Arrange
            var equipeId = 1;
            var solicitanteId = 10;
            var usuarioAdicionadoId = 20;

            var equipe = new Equipe
            {
                Id = equipeId,
                Nome = "Equipe Alpha"
            };

            var solicitanteMembro = new MembroEquipe
            {
                Id = 1,
                EquipeId = equipeId,
                UsuarioId = solicitanteId,
                Cargo = Cargo.Dono
            };

            var dto = new CriarMembroEquipeDTO
            {
                EquipeId = equipeId,
                UsuarioId = usuarioAdicionadoId,
                Cargo = Cargo.Membro
            };

            _equipeRepoMock.Setup(r => r.ObterEquipePorIdAsync(equipeId)).ReturnsAsync(equipe);
            _usuarioRepoMock.Setup(r => r.UsuarioExisteAsync(usuarioAdicionadoId)).ReturnsAsync(true);
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId)).ReturnsAsync(solicitanteMembro);
            _membroEquipeRepoMock.Setup(r => r.MembroExisteNaEquipeAsync(equipeId, usuarioAdicionadoId)).ReturnsAsync(false);
            _membroEquipeRepoMock.Setup(r => r.AdicionarMembroEquipeAsync(It.IsAny<MembroEquipe>()))
                .ReturnsAsync((MembroEquipe m) => m);

            // Act
            var resultado = await _membroEquipeService.AdicionarMembroAsync(dto, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(usuarioAdicionadoId, resultado.UsuarioId);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                usuarioAdicionadoId,
                "equipe",
                "Adicionado a uma Equipe",
                It.Is<string>(msg => msg.Contains("Equipe Alpha")),
                $"/equipes/{equipeId}",
                false,
                null), Times.Once);
        }

        [Fact]
        public async Task AdicionarMembro_EquipeNaoEncontrada_DeveLancarExcecao()
        {
            // Arrange
            var dto = new CriarMembroEquipeDTO
            {
                EquipeId = 999,
                UsuarioId = 2
            };

            _equipeRepoMock.Setup(r => r.ObterEquipePorIdAsync(999)).ReturnsAsync((Equipe?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _membroEquipeService.AdicionarMembroAsync(dto, solicitanteId: 1));
        }
    }
}

