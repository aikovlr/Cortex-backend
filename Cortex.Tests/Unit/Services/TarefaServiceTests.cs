using Cortex.DTOs.Request;
using Cortex.Entities;
using Cortex.Repositories.Interface;
using Cortex.Services.Implementations;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class TarefaServiceTests
    {
        private readonly Mock<ITarefaRepository> _tarefaRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IMetaService> _metaServiceMock;
        private readonly Mock<IEquipeAuthorizationService> _authServiceMock;
        private readonly Mock<IArquivoValidacaoService> _arquivoValidacaoServiceMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<ILogger<TarefaService>> _loggerMock;
        private readonly TarefaService _tarefaService;

        public TarefaServiceTests()
        {
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _metaServiceMock = new Mock<IMetaService>();
            _authServiceMock = new Mock<IEquipeAuthorizationService>();
            _arquivoValidacaoServiceMock = new Mock<IArquivoValidacaoService>();
            _envMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<TarefaService>>();

            _tarefaService = new TarefaService(
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _metaServiceMock.Object,
                _authServiceMock.Object,
                _arquivoValidacaoServiceMock.Object,
                _envMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CriarTarefa_DeveCriarComStatusPendente()
        {
            // Arrange
            var criadorId = 1;
            var criador = new Usuario
            {
                Id = criadorId,
                Nome = "Criador",
                Email = "criador@exemplo.com",
                SenhaHash = "hash"
            };

            var dto = new CriarTarefaDTO
            {
                Titulo = "Nova Tarefa",
                DataVencimento = DateTime.UtcNow.AddDays(3),
                Prioridade = Prioridade.Alta
            };

            _usuarioRepoMock
                .Setup(r => r.ObterUsuarioPorIdAsync(criadorId))
                .ReturnsAsync(criador);

            _tarefaRepoMock
                .Setup(r => r.AdicionarTarefaAsync(It.IsAny<Tarefa>()))
                .ReturnsAsync((Tarefa t) => t);

            // Act
            var resultado = await _tarefaService.CriarTarefaAsync(dto, criadorId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("Nova Tarefa", resultado.Titulo);
            Assert.Equal(Status.Pendente, resultado.Status);
        }

        [Fact]
        public async Task ConcluirTarefa_RN08_DeveMarcarMetasVinculadasComoConcluidas()
        {
            // Arrange
            var tarefaId = 10;
            var solicitanteId = 1;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa a Concluir",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Status = Status.Pendente,
                Prioridade = Prioridade.Media,
                CriadorId = solicitanteId,
                Criador = new Usuario { Id = solicitanteId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
            };

            var metaVinculada = new Meta
            {
                Id = 100,
                Nome = "Meta Vinculada",
                TarefaId = tarefaId,
                Concluida = false
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(solicitanteId)).ReturnsAsync(tarefa.Criador);

            // Act
            var resultado = await _tarefaService.ConcluirTarefaAsync(tarefaId, solicitanteId);

            // Assert
            Assert.Equal(Status.Concluida, resultado.Status);
            _metaServiceMock.Verify(m => m.VerificarConclusaoMetaAsync(tarefaId), Times.Once);
        }

        [Fact]
        public async Task VerificarTarefasAtrasadas_RN06_DeveTransicionarTarefasVencidasParaAtrasadas()
        {
            // Arrange
            var tarefasAtrasadas = new List<Tarefa>
            {
                new Tarefa
                {
                    Id = 1,
                    Titulo = "Vencida 1",
                    DataVencimento = DateTime.UtcNow.AddHours(-2),
                    Status = Status.Pendente,
                    Prioridade = Prioridade.Media,
                    CriadorId = 1,
                    Criador = new Usuario { Id = 1, Nome = "C", Email = "c@test.com", SenhaHash = "hash" }
                },
                new Tarefa
                {
                    Id = 2,
                    Titulo = "Vencida 2",
                    DataVencimento = DateTime.UtcNow.AddDays(-1),
                    Status = Status.Pendente,
                    Prioridade = Prioridade.Alta,
                    CriadorId = 1,
                    Criador = new Usuario { Id = 1, Nome = "C", Email = "c@test.com", SenhaHash = "hash" }
                }
            };

            _tarefaRepoMock
                .Setup(r => r.ObterTarefasVencidasNaoConcluidasAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(tarefasAtrasadas);

            // Act
            var totalAtrasadas = await _tarefaService.VerificarTarefasAtrasadasAsync();

            // Assert
            Assert.Equal(2, totalAtrasadas);
            Assert.All(tarefasAtrasadas, t => Assert.Equal(Status.Atrasada, t.Status));
            _tarefaRepoMock.Verify(r => r.AtualizarTarefaAsync(It.IsAny<Tarefa>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AtualizarTarefa_PatchParcial_DeveAtualizarApenasCamposInformados()
        {
            // Arrange
            var dataOriginal = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);
            var tarefaOriginal = new Tarefa
            {
                Id = 10,
                Titulo = "Titulo Original",
                Descricao = "Descricao Original",
                DataVencimento = dataOriginal,
                Pontuacao = 50,
                Prioridade = Prioridade.Baixa,
                CriadorId = 1,
                Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>()
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(10))
                .ReturnsAsync(tarefaOriginal);

            // Solicitante é o criador
            var dto = new AtualizarTarefaDTO
            {
                Titulo = "Titulo Atualizado",
                Descricao = null, // Não deve sobrescrever
                DataVencimento = null, // Não deve sobrescrever
                Pontuacao = null, // Não deve sobrescrever
                Prioridade = null // Não deve sobrescrever
            };

            // Act
            var resultado = await _tarefaService.AtualizarTarefaAsync(10, dto, solicitanteId: 1);

            // Assert
            Assert.Equal("Titulo Atualizado", tarefaOriginal.Titulo);
            Assert.Equal("Descricao Original", tarefaOriginal.Descricao);
            Assert.Equal(dataOriginal, tarefaOriginal.DataVencimento);
            Assert.Equal(50, tarefaOriginal.Pontuacao);
            Assert.Equal(Prioridade.Baixa, tarefaOriginal.Prioridade);
            _tarefaRepoMock.Verify(r => r.AtualizarTarefaAsync(tarefaOriginal), Times.Once);
        }
    }
}
