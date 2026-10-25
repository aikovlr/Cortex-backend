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
    public class RelatorioServiceTests
    {
        private readonly Mock<IRelatorioRepository> _relatorioRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEquipeRepository> _equipeRepoMock;
        private readonly Mock<IEquipeAuthorizationService> _authServiceMock;
        private readonly Mock<ILogger<RelatorioService>> _loggerMock;
        private readonly RelatorioService _service;

        public RelatorioServiceTests()
        {
            _relatorioRepoMock = new Mock<IRelatorioRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _equipeRepoMock = new Mock<IEquipeRepository>();
            _authServiceMock = new Mock<IEquipeAuthorizationService>();
            _loggerMock = new Mock<ILogger<RelatorioService>>();

            _service = new RelatorioService(
                _relatorioRepoMock.Object,
                _usuarioRepoMock.Object,
                _equipeRepoMock.Object,
                _authServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GerarRelatorioTarefas_CalculaMetricasCorretamente()
        {
            // Arrange
            var solicitanteId = 1;
            var usuario = new Usuario
            {
                Id = solicitanteId,
                Nome = "Membro",
                Email = "membro@test.com",
                SenhaHash = "hash"
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(solicitanteId))
                .ReturnsAsync(usuario);

            var criador = new Usuario { Id = solicitanteId, Nome = "Membro", Email = "membro@test.com", SenhaHash = "hash" };

            var tarefas = new List<Tarefa>
            {
                new() { Id = 1, Titulo = "T1", DataVencimento = DateTime.UtcNow, CriadorId = solicitanteId, Criador = criador, Status = Status.Concluida, Prioridade = Prioridade.Alta },
                new() { Id = 2, Titulo = "T2", DataVencimento = DateTime.UtcNow, CriadorId = solicitanteId, Criador = criador, Status = Status.Pendente, Prioridade = Prioridade.Media },
                new() { Id = 3, Titulo = "T3", DataVencimento = DateTime.UtcNow, CriadorId = solicitanteId, Criador = criador, Status = Status.Atrasada, Prioridade = Prioridade.Baixa }
            };

            var filtro = new FiltroRelatorioDTO();
            _relatorioRepoMock.Setup(r => r.ObterTarefasParaRelatorioAsync(solicitanteId, false, filtro))
                .ReturnsAsync(tarefas);

            // Act
            var relatorio = await _service.GerarRelatorioTarefasAsync(filtro, solicitanteId);

            // Assert
            Assert.Equal(3, relatorio.TotalTarefas);
            Assert.Equal(1, relatorio.Concluidas);
            Assert.Equal(1, relatorio.Pendentes);
            Assert.Equal(1, relatorio.Atrasadas);
            Assert.Equal(33.33, relatorio.TaxaConclusaoPercentual);
        }

        [Fact]
        public async Task GerarRelatorioEquipe_NaoAdminOuDono_LancaForbiddenException()
        {
            // Arrange
            var equipeId = 10;
            var solicitanteId = 2;
            var equipe = new Equipe { Id = equipeId, Nome = "Equipe 10", DataCriacao = DateTime.UtcNow };

            _equipeRepoMock.Setup(r => r.ObterEquipePorIdAsync(equipeId))
                .ReturnsAsync(equipe);

            _authServiceMock.Setup(a => a.ExigirAdminOuDonoDaEquipeAsync(solicitanteId, equipeId))
                .ThrowsAsync(new ForbiddenException("Apenas o Dono ou Administrador da equipe possuem permissão para esta operação."));

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.GerarRelatorioEquipeAsync(equipeId, new FiltroRelatorioDTO(), solicitanteId));
        }
    }
}
