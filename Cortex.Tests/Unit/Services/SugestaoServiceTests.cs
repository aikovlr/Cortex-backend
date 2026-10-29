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
    public class SugestaoServiceTests
    {
        private readonly Mock<ISugestaoRepository> _sugestaoRepoMock;
        private readonly Mock<ITarefaRepository> _tarefaRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEquipeAuthorizationService> _authServiceMock;
        private readonly Mock<INotificacaoService> _notificacaoServiceMock;
        private readonly Mock<ILogger<SugestaoService>> _loggerMock;
        private readonly SugestaoService _service;

        public SugestaoServiceTests()
        {
            _sugestaoRepoMock = new Mock<ISugestaoRepository>();
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _authServiceMock = new Mock<IEquipeAuthorizationService>();
            _notificacaoServiceMock = new Mock<INotificacaoService>();
            _loggerMock = new Mock<ILogger<SugestaoService>>();

            _service = new SugestaoService(
                _sugestaoRepoMock.Object,
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _authServiceMock.Object,
                _notificacaoServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task AprovarSugestao_RN14_DeveMudarStatusParaAprovadaEAtualizarTarefa()
        {
            var criadorId = 1;
            var tarefa = new Tarefa
            {
                Id = 10,
                Titulo = "Tarefa Original",
                Descricao = "Descricao Original",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                CriadorId = criadorId,
                Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
            };

            var sugestao = new Sugestao
            {
                Id = 100,
                Descricao = "Nova Descricao Proposta",
                Status = StatusSugestao.Pendente,
                TarefaId = 10,
                Tarefa = tarefa,
                UsuarioId = 2
            };

            _sugestaoRepoMock.Setup(r => r.ObterSugestaoPorIdAsync(100)).ReturnsAsync(sugestao);

            var resultado = await _service.AprovarSugestaoAsync(100, criadorId);

            Assert.Equal(StatusSugestao.Aprovada, resultado.Status);
            Assert.Equal(StatusSugestao.Aprovada, sugestao.Status);
            Assert.Contains("Nova Descricao Proposta", tarefa.Descricao);
            _sugestaoRepoMock.Verify(r => r.AtualizarSugestaoAsync(sugestao), Times.Once);
            _tarefaRepoMock.Verify(r => r.AtualizarTarefaAsync(tarefa), Times.Once);
        }

        [Fact]
        public async Task AprovarSugestao_JaProcessada_DeveLancarInvalidOperationException()
        {
            var criadorId = 1;
            var sugestao = new Sugestao
            {
                Id = 100,
                Descricao = "Proposta",
                Status = StatusSugestao.Aprovada,
                TarefaId = 10,
                Tarefa = new Tarefa
                {
                    Id = 10,
                    Titulo = "Tarefa",
                    DataVencimento = DateTime.UtcNow.AddDays(1),
                    CriadorId = criadorId,
                    Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
                },
                UsuarioId = 2
            };

            _sugestaoRepoMock.Setup(r => r.ObterSugestaoPorIdAsync(100)).ReturnsAsync(sugestao);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AprovarSugestaoAsync(100, criadorId));
        }

        [Fact]
        public async Task RejeitarSugestao_DeveMudarStatusParaRejeitada()
        {
            var criadorId = 1;
            var sugestao = new Sugestao
            {
                Id = 100,
                Descricao = "Proposta",
                Status = StatusSugestao.Pendente,
                TarefaId = 10,
                Tarefa = new Tarefa
                {
                    Id = 10,
                    Titulo = "Tarefa",
                    DataVencimento = DateTime.UtcNow.AddDays(1),
                    CriadorId = criadorId,
                    Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
                },
                UsuarioId = 2
            };

            _sugestaoRepoMock.Setup(r => r.ObterSugestaoPorIdAsync(100)).ReturnsAsync(sugestao);

            var resultado = await _service.RejeitarSugestaoAsync(100, criadorId);

            Assert.Equal(StatusSugestao.Rejeitada, resultado.Status);
            Assert.Equal(StatusSugestao.Rejeitada, sugestao.Status);
            _sugestaoRepoMock.Verify(r => r.AtualizarSugestaoAsync(sugestao), Times.Once);
        }

        [Fact]
        public async Task AprovarSugestao_NaoCriador_DeveLancarForbiddenException()
        {
            var criadorId = 1;
            var outroUsuarioId = 2;
            var sugestao = new Sugestao
            {
                Id = 100,
                Descricao = "Proposta",
                Status = StatusSugestao.Pendente,
                TarefaId = 10,
                Tarefa = new Tarefa
                {
                    Id = 10,
                    Titulo = "Tarefa",
                    DataVencimento = DateTime.UtcNow.AddDays(1),
                    CriadorId = criadorId,
                    Criador = new Usuario { Id = criadorId, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
                },
                UsuarioId = outroUsuarioId
            };

            _sugestaoRepoMock.Setup(r => r.ObterSugestaoPorIdAsync(100)).ReturnsAsync(sugestao);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.AprovarSugestaoAsync(100, outroUsuarioId));
        }
    }
}
