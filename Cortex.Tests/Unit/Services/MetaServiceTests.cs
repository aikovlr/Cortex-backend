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
    public class MetaServiceTests
    {
        private readonly Mock<IMetaRepository> _metaRepoMock;
        private readonly Mock<ITarefaRepository> _tarefaRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEquipeAuthorizationService> _authServiceMock;
        private readonly Mock<ILogger<MetaService>> _loggerMock;
        private readonly MetaService _metaService;

        public MetaServiceTests()
        {
            _metaRepoMock = new Mock<IMetaRepository>();
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _authServiceMock = new Mock<IEquipeAuthorizationService>();
            _loggerMock = new Mock<ILogger<MetaService>>();

            _metaService = new MetaService(
                _metaRepoMock.Object,
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _authServiceMock.Object,
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

        [Fact]
        public async Task VerificarConclusaoMeta_RN08_DeveConcluirMeta_QuandoTodasTarefasEstiveremConcluidas()
        {
            // Arrange
            var tarefaId = 1;
            var meta = new Meta { Id = 10, Nome = "Meta Teste", TarefaId = tarefaId, Concluida = false };
            var criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" };
            var tarefaConcluida = new Tarefa 
            { 
                Id = tarefaId, 
                Titulo = "T1", 
                Status = Status.Concluida,
                DataVencimento = DateTime.UtcNow.AddDays(1),
                CriadorId = 1,
                Criador = criador
            };

            _metaRepoMock.Setup(r => r.ObterMetasPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<Meta> { meta });
            _metaRepoMock.Setup(r => r.ObterTarefasDaMetaAsync(meta.Id))
                .ReturnsAsync(new List<Tarefa> { tarefaConcluida });

            // Act
            var totalConcluidas = await _metaService.VerificarConclusaoMetaAsync(tarefaId);

            // Assert
            Assert.Equal(1, totalConcluidas);
            Assert.True(meta.Concluida);
            _metaRepoMock.Verify(r => r.AtualizarMetaAsync(meta), Times.Once);
        }

        [Fact]
        public async Task VerificarConclusaoMeta_RN08_NaoDeveConcluirMeta_QuandoHouverTarefaNaoConcluida()
        {
            // Arrange
            var tarefaId = 1;
            var meta = new Meta { Id = 10, Nome = "Meta Teste", TarefaId = tarefaId, Concluida = false };
            var criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" };
            var tarefaPendente = new Tarefa 
            { 
                Id = tarefaId, 
                Titulo = "T1", 
                Status = Status.Pendente,
                DataVencimento = DateTime.UtcNow.AddDays(1),
                CriadorId = 1,
                Criador = criador
            };

            _metaRepoMock.Setup(r => r.ObterMetasPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<Meta> { meta });
            _metaRepoMock.Setup(r => r.ObterTarefasDaMetaAsync(meta.Id))
                .ReturnsAsync(new List<Tarefa> { tarefaPendente });

            // Act
            var totalConcluidas = await _metaService.VerificarConclusaoMetaAsync(tarefaId);

            // Assert
            Assert.Equal(0, totalConcluidas);
            Assert.False(meta.Concluida);
            _metaRepoMock.Verify(r => r.AtualizarMetaAsync(It.IsAny<Meta>()), Times.Never);
        }

        [Fact]
        public async Task AtualizarMeta_DeveAtualizarTituloDataETarefasComSucesso()
        {
            // Arrange
            var solicitanteId = 1;
            var metaId = 10;
            var criador = new Usuario { Id = solicitanteId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" };
            var tarefaAntiga = new Tarefa
            {
                Id = 1,
                Titulo = "Tarefa 1",
                Status = Status.Pendente,
                DataVencimento = DateTime.UtcNow.AddDays(1),
                CriadorId = solicitanteId,
                Criador = criador
            };
            var tarefaNova = new Tarefa
            {
                Id = 2,
                Titulo = "Tarefa 2",
                Status = Status.Concluida,
                DataVencimento = DateTime.UtcNow.AddDays(2),
                CriadorId = solicitanteId,
                Criador = criador
            };

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Nome Antigo",
                DataFechamento = DateTime.UtcNow.AddMonths(1),
                TarefaId = 1,
                UsuarioId = solicitanteId,
                Tarefa = tarefaAntiga,
                Concluida = false,
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa { Id = 1, MetaId = metaId, TarefaId = 1, Tarefa = tarefaAntiga }
                }
            };

            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(metaId)).ReturnsAsync(meta);
            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(2)).ReturnsAsync(tarefaNova);
            _metaRepoMock.Setup(r => r.AtualizarMetaAsync(It.IsAny<Meta>())).ReturnsAsync((Meta m) => m);

            var novaData = DateTime.UtcNow.AddMonths(2);
            var dto = new AtualizarMetaDTO
            {
                Nome = "Novo Nome da Meta",
                DataFechamento = novaData,
                TarefaIds = new List<int> { 2 }
            };

            // Act
            var resultado = await _metaService.AtualizarMetaAsync(metaId, dto, solicitanteId);

            // Assert
            Assert.Equal("Novo Nome da Meta", meta.Nome);
            Assert.True(meta.Concluida); // Tarefa 2 está Concluída, logo meta torna-se concluída
            Assert.Equal(2, meta.TarefaId);
            Assert.Single(meta.MetasTarefas);
            Assert.Equal(2, meta.MetasTarefas.First().TarefaId);
            _metaRepoMock.Verify(r => r.AtualizarMetaAsync(meta), Times.Once);
        }

        [Fact]
        public async Task AtualizarMeta_DeveLancarExcecaoSeSemPermissao()
        {
            // Arrange
            var solicitanteId = 99;
            var metaId = 10;
            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta de Outro",
                UsuarioId = 1,
                TarefaId = 1
            };

            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(metaId)).ReturnsAsync(meta);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDaTarefaAsync(solicitanteId, It.IsAny<Tarefa>())).ReturnsAsync(false);

            var dto = new AtualizarMetaDTO { Nome = "Hacker" };

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _metaService.AtualizarMetaAsync(metaId, dto, solicitanteId));
        }
    }
}
