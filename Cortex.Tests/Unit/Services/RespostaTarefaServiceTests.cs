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
    public class RespostaTarefaServiceTests
    {
        private readonly Mock<IRespostaTarefaRepository> _respostaRepoMock;
        private readonly Mock<ITarefaRepository> _tarefaRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IAnexoRepository> _anexoRepoMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<INotificacaoService> _notificacaoServiceMock;
        private readonly Mock<ILogger<RespostaTarefaService>> _loggerMock;
        private readonly RespostaTarefaService _respostaService;

        public RespostaTarefaServiceTests()
        {
            _respostaRepoMock = new Mock<IRespostaTarefaRepository>();
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _anexoRepoMock = new Mock<IAnexoRepository>();
            _envMock = new Mock<IWebHostEnvironment>();
            _notificacaoServiceMock = new Mock<INotificacaoService>();
            _loggerMock = new Mock<ILogger<RespostaTarefaService>>();

            _respostaService = new RespostaTarefaService(
                _respostaRepoMock.Object,
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _anexoRepoMock.Object,
                _envMock.Object,
                _loggerMock.Object,
                _notificacaoServiceMock.Object);
        }

        [Fact]
        public async Task ListarRespostasPorTarefaAsync_TarefaConcluidaSemResposta_DeveCriarEListarRespostaDoResponsavel()
        {
            // Arrange
            var tarefaId = 50;
            var responsavelId = 12;
            var solicitanteId = 1;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa Concluída",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Status = Status.Concluida,
                Prioridade = Prioridade.Media,
                CriadorId = solicitanteId,
                Criador = new Usuario { Id = solicitanteId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa
                    {
                        Id = 1,
                        TarefaId = tarefaId,
                        UsuarioId = responsavelId,
                        Usuario = new Usuario { Id = responsavelId, Nome = "Colaborador Sem Anexo", Email = "colab@test.com", SenhaHash = "hash" }
                    }
                }
            };

            var respostasRetornadas = new List<RespostaTarefa>
            {
                new RespostaTarefa
                {
                    Id = 200,
                    TarefaId = tarefaId,
                    UsuarioId = responsavelId,
                    Usuario = tarefa.ResponsaveisTarefas.First().Usuario,
                    Anexos = new List<Anexo>()
                }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _respostaRepoMock.SetupSequence(r => r.ObterRespostasTarefaPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<RespostaTarefa>()) // Primeira consulta: ainda não tem resposta
                .ReturnsAsync(respostasRetornadas);      // Segunda consulta: após adicionar

            // Act
            var resultado = (await _respostaService.ListarRespostasPorTarefaAsync(tarefaId, solicitanteId)).ToList();

            // Assert
            Assert.Single(resultado);
            Assert.Equal(responsavelId, resultado[0].UsuarioId);
            _respostaRepoMock.Verify(r => r.AdicionarRespostaTarefaAsync(It.Is<RespostaTarefa>(
                resp => resp.TarefaId == tarefaId && resp.UsuarioId == responsavelId)), Times.Once);
        }

        [Fact]
        public async Task ListarRespostasPorTarefaAsync_TarefaPendente_NaoDeveCriarRespostaAutomatica()
        {
            // Arrange
            var tarefaId = 51;
            var responsavelId = 15;
            var solicitanteId = 1;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa Pendente",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Status = Status.Pendente,
                Prioridade = Prioridade.Media,
                CriadorId = solicitanteId,
                Criador = new Usuario { Id = solicitanteId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = responsavelId }
                }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _respostaRepoMock.Setup(r => r.ObterRespostasTarefaPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<RespostaTarefa>());

            // Act
            var resultado = (await _respostaService.ListarRespostasPorTarefaAsync(tarefaId, solicitanteId)).ToList();

            // Assert
            Assert.Empty(resultado);
            _respostaRepoMock.Verify(r => r.AdicionarRespostaTarefaAsync(It.IsAny<RespostaTarefa>()), Times.Never);
        }

        [Fact]
        public async Task CriarRespostaAsync_DeveNotificarCriadorDaTarefa()
        {
            // Arrange
            var tarefaId = 60;
            var criadorId = 1;
            var solicitanteId = 2;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa com Resposta",
                DataVencimento = DateTime.UtcNow.AddDays(2),
                CriadorId = criadorId,
                Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "criador@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = solicitanteId }
                }
            };

            var solicitante = new Usuario
            {
                Id = solicitanteId,
                Nome = "Responsável Fulano",
                Email = "fulano@test.com",
                SenhaHash = "hash"
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(solicitanteId)).ReturnsAsync(solicitante);

            var dto = new Cortex.DTOs.Request.CriarRespostaTarefaDTO
            {
                TarefaId = tarefaId
            };

            // Act
            await _respostaService.CriarRespostaAsync(dto, solicitanteId);

            // Assert
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                criadorId,
                "tarefa",
                "Nova Resposta Recebida",
                It.Is<string>(msg => msg.Contains("Responsável Fulano") && msg.Contains("Tarefa com Resposta")),
                $"/tarefas-atribuidas/{tarefaId}",
                false,
                tarefaId), Times.Once);
        }

        [Fact]
        public async Task AtribuirNotaAsync_DeveNotificarResponsavelDaResposta()
        {
            // Arrange
            var respostaId = 88;
            var tarefaId = 65;
            var criadorId = 1;
            var responsavelId = 3;

            var resposta = new RespostaTarefa
            {
                Id = respostaId,
                TarefaId = tarefaId,
                UsuarioId = responsavelId
            };

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa Avaliada",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Pontuacao = 100,
                CriadorId = criadorId,
                Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
            };

            _respostaRepoMock.Setup(r => r.ObterRespostaTarefaPorIdAsync(respostaId)).ReturnsAsync(resposta);
            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);

            // Act
            await _respostaService.AtribuirNotaAsync(respostaId, 85, criadorId);

            // Assert
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                responsavelId,
                "tarefa",
                "Avaliação de Resposta Recebida",
                It.Is<string>(msg => msg.Contains("85") && msg.Contains("Tarefa Avaliada")),
                $"/minhas-tarefas/{tarefaId}",
                false,
                tarefaId), Times.Once);
        }
    }
}

