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
    public class ResponsavelTarefaServiceTests
    {
        private readonly Mock<IResponsavelTarefaRepository> _responsavelTarefaRepoMock;
        private readonly Mock<ITarefaRepository> _tarefaRepoMock;
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IEquipeRepository> _equipeRepoMock;
        private readonly Mock<IMembroEquipeRepository> _membroEquipeRepoMock;
        private readonly Mock<INotificacaoService> _notificacaoServiceMock;
        private readonly Mock<ILogger<ResponsavelTarefaService>> _loggerMock;
        private readonly ResponsavelTarefaService _service;

        public ResponsavelTarefaServiceTests()
        {
            _responsavelTarefaRepoMock = new Mock<IResponsavelTarefaRepository>();
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _equipeRepoMock = new Mock<IEquipeRepository>();
            _membroEquipeRepoMock = new Mock<IMembroEquipeRepository>();
            _notificacaoServiceMock = new Mock<INotificacaoService>();
            _loggerMock = new Mock<ILogger<ResponsavelTarefaService>>();

            _service = new ResponsavelTarefaService(
                _responsavelTarefaRepoMock.Object,
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _equipeRepoMock.Object,
                _membroEquipeRepoMock.Object,
                _notificacaoServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task AtribuirEquipeAsync_QuandoEquipeContemCriador_NaoDeveIncluirCriadorNosResponsaveis()
        {
            // Arrange
            var tarefaId = 1;
            var equipeId = 10;
            var criadorId = 99;
            var outroMembroId = 100;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa Teste",
                CriadorId = criadorId,
                Criador = new Usuario
                {
                    Id = criadorId,
                    Nome = "Criador Teste",
                    Email = "criador@teste.com",
                    SenhaHash = "hash"
                },
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Prioridade = Prioridade.Media
            };

            var membros = new List<MembroEquipe>
            {
                new MembroEquipe { EquipeId = equipeId, UsuarioId = criadorId, Cargo = Cargo.Dono },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = outroMembroId, Cargo = Cargo.Membro }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId))
                .ReturnsAsync(tarefa);

            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId))
                .ReturnsAsync(true);

            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, criadorId))
                .ReturnsAsync(new MembroEquipe { EquipeId = equipeId, UsuarioId = criadorId, Cargo = Cargo.Dono });

            _membroEquipeRepoMock.Setup(r => r.ObterMembrosEquipePorEquipeIdAsync(equipeId))
                .ReturnsAsync(membros);

            _responsavelTarefaRepoMock.Setup(r => r.ResponsavelJaAtribuidoAsync(tarefaId, outroMembroId))
                .ReturnsAsync(false);

            List<ResponsavelTarefa>? adicionadosEmLote = null;
            _responsavelTarefaRepoMock
                .Setup(r => r.AdicionarResponsaveisEmLoteAsync(It.IsAny<IEnumerable<ResponsavelTarefa>>()))
                .Callback<IEnumerable<ResponsavelTarefa>>(l => adicionadosEmLote = l.ToList())
                .Returns(Task.CompletedTask);

            _responsavelTarefaRepoMock.Setup(r => r.ObterResponsaveisPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = outroMembroId }
                });

            // Act
            var resultado = await _service.AtribuirEquipeAsync(tarefaId, equipeId, criadorId);

            // Assert
            Assert.NotNull(adicionadosEmLote);
            Assert.Single(adicionadosEmLote);
            Assert.Equal(outroMembroId, adicionadosEmLote[0].UsuarioId);
            Assert.DoesNotContain(adicionadosEmLote, r => r.UsuarioId == criadorId);

            // Verifica que o criador não foi notificado
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                criadorId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int?>()), Times.Never);

            // Verifica que o outro membro foi notificado
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                outroMembroId,
                "tarefa",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                tarefaId), Times.Once);
        }

        [Fact]
        public async Task AtribuirEquipeAsync_QuandoSolicitanteEhApenasMembro_DeveLancarForbiddenException()
        {
            // Arrange
            var tarefaId = 1;
            var equipeId = 10;
            var solicitanteId = 99;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa",
                CriadorId = solicitanteId,
                Criador = new Usuario
                {
                    Id = solicitanteId,
                    Nome = "Solicitante",
                    Email = "sol@test.com",
                    SenhaHash = "hash"
                },
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Prioridade = Prioridade.Media
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId))
                .ReturnsAsync(tarefa);

            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId))
                .ReturnsAsync(true);

            // Solicitante é apenas Cargo.Membro na equipe
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId))
                .ReturnsAsync(new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Membro });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.AtribuirEquipeAsync(tarefaId, equipeId, solicitanteId));

            Assert.Contains("Apenas o Dono ou Administradores da equipe", ex.Message);
        }

        [Fact]
        public async Task AtribuirEquipeAsync_QuandoSolicitanteNaoPertenceAEquipe_DeveLancarForbiddenException()
        {
            // Arrange
            var tarefaId = 1;
            var equipeId = 10;
            var solicitanteId = 99;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa",
                CriadorId = solicitanteId,
                Criador = new Usuario
                {
                    Id = solicitanteId,
                    Nome = "Solicitante",
                    Email = "sol@test.com",
                    SenhaHash = "hash"
                },
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Prioridade = Prioridade.Media
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId))
                .ReturnsAsync(tarefa);

            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId))
                .ReturnsAsync(true);

            // Solicitante não é membro da equipe
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId))
                .ReturnsAsync((MembroEquipe?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.AtribuirEquipeAsync(tarefaId, equipeId, solicitanteId));

            Assert.Contains("Apenas o Dono ou Administradores da equipe", ex.Message);
        }

        [Theory]
        [InlineData(Cargo.Dono)]
        [InlineData(Cargo.Admin)]
        public async Task AtribuirEquipeAsync_QuandoSolicitanteEhDonoOuAdmin_DevePermitirAtribuicao(Cargo cargo)
        {
            // Arrange
            var tarefaId = 1;
            var equipeId = 10;
            var solicitanteId = 99;
            var outroMembroId = 100;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa",
                CriadorId = solicitanteId,
                Criador = new Usuario
                {
                    Id = solicitanteId,
                    Nome = "Solicitante",
                    Email = "sol@test.com",
                    SenhaHash = "hash"
                },
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Prioridade = Prioridade.Media
            };

            var membros = new List<MembroEquipe>
            {
                new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = cargo },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = outroMembroId, Cargo = Cargo.Membro }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId))
                .ReturnsAsync(tarefa);

            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId))
                .ReturnsAsync(true);

            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId))
                .ReturnsAsync(new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = cargo });

            _membroEquipeRepoMock.Setup(r => r.ObterMembrosEquipePorEquipeIdAsync(equipeId))
                .ReturnsAsync(membros);

            _responsavelTarefaRepoMock.Setup(r => r.ResponsavelJaAtribuidoAsync(tarefaId, outroMembroId))
                .ReturnsAsync(false);

            _responsavelTarefaRepoMock
                .Setup(r => r.AdicionarResponsaveisEmLoteAsync(It.IsAny<IEnumerable<ResponsavelTarefa>>()))
                .Returns(Task.CompletedTask);

            _responsavelTarefaRepoMock.Setup(r => r.ObterResponsaveisPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<ResponsavelTarefa>
                {
                    new ResponsavelTarefa { Id = 1, TarefaId = tarefaId, UsuarioId = outroMembroId }
                });

            // Act
            var resultado = await _service.AtribuirEquipeAsync(tarefaId, equipeId, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            _responsavelTarefaRepoMock.Verify(r => r.AdicionarResponsaveisEmLoteAsync(It.IsAny<IEnumerable<ResponsavelTarefa>>()), Times.Once);
        }

        [Fact]
        public async Task AtribuirEquipeAsync_Hierarquia_QuandoSolicitanteEhAdmin_ApenasMembrosDevemSerAtribuidos()
        {
            // Arrange
            var tarefaId = 1;
            var equipeId = 10;
            var solicitanteId = 99;
            var donoId = 101;
            var outroAdminId = 102;
            var membroId = 103;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa",
                CriadorId = solicitanteId,
                Criador = new Usuario
                {
                    Id = solicitanteId,
                    Nome = "Admin Solicitante",
                    Email = "admin@test.com",
                    SenhaHash = "hash"
                },
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Prioridade = Prioridade.Media
            };

            var membros = new List<MembroEquipe>
            {
                new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Admin },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = donoId, Cargo = Cargo.Dono },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = outroAdminId, Cargo = Cargo.Admin },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = membroId, Cargo = Cargo.Membro }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId)).ReturnsAsync(true);
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId))
                .ReturnsAsync(new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Admin });
            _membroEquipeRepoMock.Setup(r => r.ObterMembrosEquipePorEquipeIdAsync(equipeId)).ReturnsAsync(membros);

            List<ResponsavelTarefa>? adicionados = null;
            _responsavelTarefaRepoMock
                .Setup(r => r.AdicionarResponsaveisEmLoteAsync(It.IsAny<IEnumerable<ResponsavelTarefa>>()))
                .Callback<IEnumerable<ResponsavelTarefa>>(l => adicionados = l.ToList())
                .Returns(Task.CompletedTask);

            _responsavelTarefaRepoMock.Setup(r => r.ObterResponsaveisPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<ResponsavelTarefa>());

            // Act
            await _service.AtribuirEquipeAsync(tarefaId, equipeId, solicitanteId);

            // Assert: Apenas o Membro deve ser atribuído; Dono e outro Admin devem ser ignorados pela hierarquia Dono > Admin > Membro
            Assert.NotNull(adicionados);
            Assert.Single(adicionados);
            Assert.Equal(membroId, adicionados[0].UsuarioId);
            Assert.DoesNotContain(adicionados, r => r.UsuarioId == donoId);
            Assert.DoesNotContain(adicionados, r => r.UsuarioId == outroAdminId);
            Assert.DoesNotContain(adicionados, r => r.UsuarioId == solicitanteId);
        }

        [Fact]
        public async Task AtribuirEquipeAsync_Hierarquia_QuandoSolicitanteEhDono_DeveAtribuirAdminEMembroMasNaoOutroDono()
        {
            // Arrange
            var tarefaId = 1;
            var equipeId = 10;
            var solicitanteId = 99;
            var outroDonoId = 101;
            var adminId = 102;
            var membroId = 103;

            var tarefa = new Tarefa
            {
                Id = tarefaId,
                Titulo = "Tarefa",
                CriadorId = solicitanteId,
                Criador = new Usuario
                {
                    Id = solicitanteId,
                    Nome = "Dono Solicitante",
                    Email = "dono@test.com",
                    SenhaHash = "hash"
                },
                DataVencimento = DateTime.UtcNow.AddDays(1),
                Prioridade = Prioridade.Media
            };

            var membros = new List<MembroEquipe>
            {
                new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Dono },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = outroDonoId, Cargo = Cargo.Dono },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = adminId, Cargo = Cargo.Admin },
                new MembroEquipe { EquipeId = equipeId, UsuarioId = membroId, Cargo = Cargo.Membro }
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(tarefaId)).ReturnsAsync(tarefa);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId)).ReturnsAsync(true);
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId))
                .ReturnsAsync(new MembroEquipe { EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Dono });
            _membroEquipeRepoMock.Setup(r => r.ObterMembrosEquipePorEquipeIdAsync(equipeId)).ReturnsAsync(membros);

            List<ResponsavelTarefa>? adicionados = null;
            _responsavelTarefaRepoMock
                .Setup(r => r.AdicionarResponsaveisEmLoteAsync(It.IsAny<IEnumerable<ResponsavelTarefa>>()))
                .Callback<IEnumerable<ResponsavelTarefa>>(l => adicionados = l.ToList())
                .Returns(Task.CompletedTask);

            _responsavelTarefaRepoMock.Setup(r => r.ObterResponsaveisPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<ResponsavelTarefa>());

            // Act
            await _service.AtribuirEquipeAsync(tarefaId, equipeId, solicitanteId);

            // Assert: Admin e Membro devem ser atribuídos; outro Dono e o próprio criador devem ser ignorados
            Assert.NotNull(adicionados);
            Assert.Equal(2, adicionados.Count);
            Assert.Contains(adicionados, r => r.UsuarioId == adminId);
            Assert.Contains(adicionados, r => r.UsuarioId == membroId);
            Assert.DoesNotContain(adicionados, r => r.UsuarioId == outroDonoId);
            Assert.DoesNotContain(adicionados, r => r.UsuarioId == solicitanteId);
        }
    }
}
