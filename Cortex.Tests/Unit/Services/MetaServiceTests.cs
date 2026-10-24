using Cortex.DTOs.Request;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class MetaServiceTests
    {
        private readonly Mock<IMetaRepository> _metaRepoMock;
        private readonly Mock<ITarefaRepository> _tarefaRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<ILogger<MetaService>> _loggerMock;
        private readonly MetaService _metaService;

        public MetaServiceTests()
        {
            _metaRepoMock = new Mock<IMetaRepository>();
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _loggerMock = new Mock<ILogger<MetaService>>();

            _metaService = new MetaService(
                _metaRepoMock.Object,
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CriarMeta_RN07_DeveLancarExcecaoSeTarefaNaoExistir()
        {
            // Arrange
            var dto = new CriarMetaDTO
            {
                Nome = "Meta Sem Tarefa",
                TarefaId = 999
            };

            _tarefaRepoMock
                .Setup(r => r.ObterTarefaPorIdAsync(dto.TarefaId))
                .ReturnsAsync((Tarefa?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _metaService.CriarMetaAsync(dto, solicitanteId: 1));

            Assert.Contains("tarefa válida", ex.Message);
        }

        [Fact]
        public async Task CriarMeta_RN15_DeveLancarExcecaoSeDataFechamentoExceder12Meses()
        {
            // Arrange
            var tarefa = new Tarefa
            {
                Id = 1,
                Titulo = "Tarefa Base",
                DataVencimento = DateTime.UtcNow.AddMonths(1),
                Status = Status.Pendente,
                Prioridade = Prioridade.Media,
                CriadorId = 1,
                Criador = new Usuario { Id = 1, Nome = "C", Email = "c@test.com", SenhaHash = "hash" }
            };

            var dto = new CriarMetaDTO
            {
                Nome = "Meta Futura Demais",
                TarefaId = 1,
                DataFechamento = DateTime.UtcNow.AddMonths(14) // > 12 meses (RN15)
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(dto.TarefaId)).ReturnsAsync(tarefa);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _metaService.CriarMetaAsync(dto, solicitanteId: 1));

            Assert.Contains("12 meses", ex.Message);
        }

        [Fact]
        public async Task CriarMeta_RN08_DeveMarcarConcluidaSeTarefaJaEstiverConcluida()
        {
            // Arrange
            var tarefa = new Tarefa
            {
                Id = 1,
                Titulo = "Tarefa Concluída",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Status = Status.Concluida,
                Prioridade = Prioridade.Media,
                CriadorId = 1,
                Criador = new Usuario { Id = 1, Nome = "C", Email = "c@test.com", SenhaHash = "hash" }
            };

            var dto = new CriarMetaDTO
            {
                Nome = "Meta de Tarefa Concluída",
                TarefaId = 1
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(dto.TarefaId)).ReturnsAsync(tarefa);
            _metaRepoMock.Setup(r => r.AdicionarMetaAsync(It.IsAny<Meta>())).ReturnsAsync((Meta m) => m);
            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Meta
            {
                Id = id,
                Nome = dto.Nome,
                TarefaId = dto.TarefaId,
                Concluida = true,
                Tarefa = tarefa
            });

            // Act
            var resultado = await _metaService.CriarMetaAsync(dto, solicitanteId: 1);

            // Assert
            Assert.True(resultado.Concluida);
        }

        [Fact]
        public async Task ObterMetaPorId_RN09_DeveLancarForbiddenSeMembroNaoForDonoDaMeta()
        {
            // Arrange
            var metaId = 10;
            var donoDaMetaId = 1;
            var outroUsuarioId = 2;

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta Privada",
                UsuarioId = donoDaMetaId,
                TarefaId = 1
            };

            var outroUsuario = new Usuario
            {
                Id = outroUsuarioId,
                Nome = "Outro",
                Email = "outro@test.com",
                SenhaHash = "hash",
                Equipes = new List<MembroEquipe>() // Apenas membro comum
            };

            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(metaId)).ReturnsAsync(meta);
            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(outroUsuarioId)).ReturnsAsync(outroUsuario);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _metaService.ObterMetaPorIdAsync(metaId, solicitanteId: outroUsuarioId));
        }
    }
}
