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
        private readonly Mock<INotificacaoService> _notificacaoServiceMock;
        private readonly Mock<IRespostaTarefaRepository> _respostaRepoMock;
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
            _notificacaoServiceMock = new Mock<INotificacaoService>();
            _respostaRepoMock = new Mock<IRespostaTarefaRepository>();
            _loggerMock = new Mock<ILogger<TarefaService>>();

            _tarefaService = new TarefaService(
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _metaServiceMock.Object,
                _authServiceMock.Object,
                _arquivoValidacaoServiceMock.Object,
                _envMock.Object,
                _loggerMock.Object,
                _notificacaoServiceMock.Object,
                _respostaRepoMock.Object);
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

        [Fact]
        public async Task ConcluirTarefa_SemAnexo_DeveCriarRespostaParaResponsavelParaAparecerNaLista()
        {
            // Arrange
            var tarefaId = 20;
            var responsavelId = 5;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa sem Anexo",
                DataVencimento = DateTime.UtcNow.AddDays(2),
                Status = Status.Pendente,
                Prioridade = Prioridade.Alta,
                CriadorId = 1,
                Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = responsavelId }
                },
                Respostas = new List<RespostaTarefa>()
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);

            // Act
            var resultado = await _tarefaService.ConcluirTarefaAsync(tarefaId, responsavelId);

            // Assert
            Assert.Equal(Status.Concluida, resultado.Status);
            _respostaRepoMock.Verify(r => r.AdicionarRespostaTarefaAsync(It.Is<RespostaTarefa>(
                resp => resp.TarefaId == tarefaId && resp.UsuarioId == responsavelId)), Times.Once);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                tarefa.CriadorId,
                "tarefa",
                "Tarefa Concluída",
                It.Is<string>(msg => msg.Contains("Tarefa sem Anexo")),
                $"/tarefas-atribuidas/{tarefaId}?aba=respostas",
                false,
                tarefaId), Times.Once);
        }

        [Fact]
        public async Task ConcluirTarefa_JaComResposta_NaoDeveDuplicarResposta()
        {
            // Arrange
            var tarefaId = 21;
            var responsavelId = 6;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa com Anexo",
                DataVencimento = DateTime.UtcNow.AddDays(2),
                Status = Status.Pendente,
                Prioridade = Prioridade.Alta,
                CriadorId = 1,
                Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = responsavelId }
                },
                Respostas = new List<RespostaTarefa>
                {
                    new RespostaTarefa
                    {
                        Id = 101,
                        TarefaId = tarefaId,
                        UsuarioId = responsavelId,
                        Anexos = new List<Anexo> { new Anexo { Id = 1, NomeOriginal = "arquivo.pdf", MimeType = "application/pdf", UrlCaminho = "/uploads/arquivo.pdf" } }
                    }
                }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);

            // Act
            var resultado = await _tarefaService.ConcluirTarefaAsync(tarefaId, responsavelId);

            // Assert
            Assert.Equal(Status.Concluida, resultado.Status);
            _respostaRepoMock.Verify(r => r.AdicionarRespostaTarefaAsync(It.IsAny<RespostaTarefa>()), Times.Never);
        }

        [Fact]
        public async Task ConcluirTarefa_MultiplosResponsaveis_DeveStackarNotificacaoComNomesDosResponsaveis()
        {
            // Arrange
            var tarefaId = 55;
            var criadorId = 1;
            var resp1Id = 10;
            var resp2Id = 11;

            var user1 = new Usuario { Id = resp1Id, Nome = "Alice", Email = "alice@test.com", SenhaHash = "h" };
            var user2 = new Usuario { Id = resp2Id, Nome = "Bob", Email = "bob@test.com", SenhaHash = "h" };

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Refatoração do Módulo",
                DataVencimento = DateTime.UtcNow.AddDays(3),
                Status = Status.Pendente,
                Prioridade = Prioridade.Alta,
                CriadorId = criadorId,
                Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "h" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = resp1Id, Usuario = user1 },
                    new ResponsavelTarefa { Id = 2, TarefaId = tarefaId, UsuarioId = resp2Id, Usuario = user2 }
                },
                Respostas = new List<RespostaTarefa>
                {
                    new RespostaTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = resp1Id, Usuario = user1 }
                }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _respostaRepoMock.Setup(r => r.ObterRespostasTarefaPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<RespostaTarefa>
                {
                    new RespostaTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = resp1Id, Usuario = user1 },
                    new RespostaTarefa { Id = 2, TarefaId = tarefaId, UsuarioId = resp2Id, Usuario = user2 }
                });

            // Act: Bob (resp2) conclui a tarefa após Alice já ter concluído
            var resultado = await _tarefaService.ConcluirTarefaAsync(tarefaId, resp2Id);

            // Assert
            Assert.Equal(Status.Concluida, resultado.Status);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                criadorId,
                "tarefa",
                It.Is<string>(t => t.Contains("(2)")),
                It.Is<string>(msg => msg.Contains("Alice") && msg.Contains("Bob")),
                $"/tarefas-atribuidas/{tarefaId}?aba=respostas",
                false,
                tarefaId), Times.Once);
        }

        [Fact]
        public async Task CancelarConclusaoTarefa_DeveRemoverRespostaAutomaticaSemAnexoESemNota()
        {
            // Arrange
            var tarefaId = 22;
            var responsavelId = 7;
            var respostaAutomaticaId = 555;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa para Cancelar",
                DataVencimento = DateTime.UtcNow.AddDays(2),
                Status = Status.Concluida,
                Prioridade = Prioridade.Media,
                CriadorId = 1,
                Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = responsavelId }
                },
                Respostas = new List<RespostaTarefa>
                {
                    new RespostaTarefa
                    {
                        Id = respostaAutomaticaId,
                        TarefaId = tarefaId,
                        UsuarioId = responsavelId,
                        Nota = null,
                        Anexos = new List<Anexo>()
                    }
                }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);

            // Act
            var resultado = await _tarefaService.CancelarConclusaoTarefaAsync(tarefaId, responsavelId);

            // Assert
            Assert.Equal(Status.Pendente, resultado.Status);
            _respostaRepoMock.Verify(r => r.RemoverRespostaTarefaAsync(respostaAutomaticaId), Times.Once);
        }

        [Fact]
        public async Task AtualizarTarefa_DeveNotificarResponsaveis()
        {
            // Arrange
            var tarefaId = 30;
            var criadorId = 1;
            var responsavelId1 = 10;
            var responsavelId2 = 20;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Titulo Original",
                DataVencimento = DateTime.UtcNow.AddDays(5),
                Status = Status.Pendente,
                Prioridade = Prioridade.Media,
                CriadorId = criadorId,
                Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = responsavelId1 },
                    new ResponsavelTarefa { Id = 2, TarefaId = tarefaId, UsuarioId = responsavelId2 }
                }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);

            var dto = new AtualizarTarefaDTO
            {
                Titulo = "Novo Titulo Atualizado"
            };

            // Act
            await _tarefaService.AtualizarTarefaAsync(tarefaId, dto, solicitanteId: criadorId);

            // Assert
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                responsavelId1,
                "tarefa",
                "Tarefa Atualizada",
                It.Is<string>(msg => msg.Contains("Novo Titulo Atualizado")),
                $"/minhas-tarefas/{tarefaId}",
                false,
                tarefaId), Times.Once);

            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                responsavelId2,
                "tarefa",
                "Tarefa Atualizada",
                It.Is<string>(msg => msg.Contains("Novo Titulo Atualizado")),
                $"/minhas-tarefas/{tarefaId}",
                false,
                tarefaId), Times.Once);
        }

        [Fact]
        public async Task ListarTarefasAsync_ComFiltroAbaTarefasConcluidas_DeveRetornarApenasConcluidas()
        {
            // Arrange
            var solicitanteId = 1;
            var criador = new Usuario { Id = solicitanteId, Nome = "Admin", Email = "admin@test.com", SenhaHash = "hash" };
            var tarefasMock = new List<Tarefa>
            {
                new Tarefa { Id = 1, Titulo = "Tarefa 1", Status = Status.Pendente, DataVencimento = DateTime.UtcNow.AddDays(1), CriadorId = solicitanteId, Criador = criador, ResponsaveisTarefas = new List<ResponsavelTarefa>() },
                new Tarefa { Id = 2, Titulo = "Tarefa 2", Status = Status.Concluida, DataVencimento = DateTime.UtcNow.AddDays(1), CriadorId = solicitanteId, Criador = criador, ResponsaveisTarefas = new List<ResponsavelTarefa>() },
                new Tarefa { Id = 3, Titulo = "Tarefa 3", Status = Status.Atrasada, DataVencimento = DateTime.UtcNow.AddDays(-1), CriadorId = solicitanteId, Criador = criador, ResponsaveisTarefas = new List<ResponsavelTarefa>() }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefasComFiltroAsync(
                It.IsAny<Status?>(),
                It.IsAny<Prioridade?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>()
            )).ReturnsAsync(tarefasMock);

            var filtros = new FiltroTarefasDTO { Aba = "tarefas-concluidas" };

            // Act
            var resultado = await _tarefaService.ListarTarefasAsync(filtros, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            var lista = resultado.ToList();
            Assert.Single(lista);
            Assert.Equal(2, lista[0].Id);
            Assert.Equal(Status.Concluida, lista[0].Status);
        }

        [Fact]
        public async Task ListarTarefasAsync_ComFiltroAbaTarefasPendentes_DeveRetornarApenasNaoConcluidas()
        {
            // Arrange
            var solicitanteId = 1;
            var criador = new Usuario { Id = solicitanteId, Nome = "Admin", Email = "admin@test.com", SenhaHash = "hash" };
            var tarefasMock = new List<Tarefa>
            {
                new Tarefa { Id = 1, Titulo = "Tarefa 1", Status = Status.Pendente, DataVencimento = DateTime.UtcNow.AddDays(1), CriadorId = solicitanteId, Criador = criador, ResponsaveisTarefas = new List<ResponsavelTarefa>() },
                new Tarefa { Id = 2, Titulo = "Tarefa 2", Status = Status.Concluida, DataVencimento = DateTime.UtcNow.AddDays(1), CriadorId = solicitanteId, Criador = criador, ResponsaveisTarefas = new List<ResponsavelTarefa>() },
                new Tarefa { Id = 3, Titulo = "Tarefa 3", Status = Status.Atrasada, DataVencimento = DateTime.UtcNow.AddDays(-1), CriadorId = solicitanteId, Criador = criador, ResponsaveisTarefas = new List<ResponsavelTarefa>() }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefasComFiltroAsync(
                It.IsAny<Status?>(),
                It.IsAny<Prioridade?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>()
            )).ReturnsAsync(tarefasMock);

            var filtros = new FiltroTarefasDTO { Aba = "tarefas-pendentes" };

            // Act
            var resultado = await _tarefaService.ListarTarefasAsync(filtros, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            var lista = resultado.ToList();
            Assert.Equal(2, lista.Count);
            Assert.DoesNotContain(lista, t => t.Status == Status.Concluida);
        }
    }
}
