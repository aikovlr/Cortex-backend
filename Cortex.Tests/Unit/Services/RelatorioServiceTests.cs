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

        [Fact]
        public async Task GerarRelatorioEquipe_ComFiltroResponsaveis_RetornaApenasMembrosFiltrados()
        {
            // Arrange
            var equipeId = 5;
            var solicitanteId = 1;
            var usuario1 = new Usuario { Id = 1, Nome = "Admin", Email = "admin@test.com", SenhaHash = "h" };
            var usuario2 = new Usuario { Id = 2, Nome = "Membro 2", Email = "m2@test.com", SenhaHash = "h" };
            var usuario3 = new Usuario { Id = 3, Nome = "Membro 3", Email = "m3@test.com", SenhaHash = "h" };

            var equipe = new Equipe
            {
                Id = equipeId,
                Nome = "Dev Team",
                DataCriacao = DateTime.UtcNow,
                Membros = new List<MembroEquipe>
                {
                    new() { EquipeId = equipeId, UsuarioId = 1, Usuario = usuario1, Cargo = Cargo.Dono },
                    new() { EquipeId = equipeId, UsuarioId = 2, Usuario = usuario2, Cargo = Cargo.Membro },
                    new() { EquipeId = equipeId, UsuarioId = 3, Usuario = usuario3, Cargo = Cargo.Membro }
                }
            };

            _equipeRepoMock.Setup(r => r.ObterEquipePorIdAsync(equipeId))
                .ReturnsAsync(equipe);
            _authServiceMock.Setup(a => a.ExigirAdminOuDonoDaEquipeAsync(solicitanteId, equipeId))
                .Returns(Task.CompletedTask);

            var tarefas = new List<Tarefa>
            {
                new()
                {
                    Id = 10,
                    Titulo = "T10",
                    Status = Status.Concluida,
                    Prioridade = Prioridade.Alta,
                    DataVencimento = DateTime.UtcNow,
                    CriadorId = 1,
                    Criador = usuario1,
                    ResponsaveisTarefas = new List<ResponsavelTarefa>
                    {
                        new() { TarefaId = 10, UsuarioId = 2, Usuario = usuario2 }
                    }
                }
            };

            var filtro = new FiltroRelatorioDTO
            {
                ResponsaveisIds = new List<int> { 2 }
            };

            _relatorioRepoMock.Setup(r => r.ObterTarefasDaEquipeAsync(equipeId, filtro))
                .ReturnsAsync(tarefas);

            // Act
            var relatorio = await _service.GerarRelatorioEquipeAsync(equipeId, filtro, solicitanteId);

            // Assert
            Assert.NotNull(relatorio);
            Assert.Single(relatorio.DesempenhoMembros);
            Assert.Equal(2, relatorio.DesempenhoMembros[0].UsuarioId);
            Assert.Equal("Membro 2", relatorio.DesempenhoMembros[0].Nome);
            Assert.Equal(1, relatorio.DesempenhoMembros[0].Concluidas);
            Assert.Equal(1, relatorio.TotalMembros);
        }

        [Fact]
        public async Task GerarRelatorioTarefas_CalculaMediaNotas_EDesempenhoResponsaveis()
        {
            // Arrange
            var solicitanteId = 1;
            var usuario = new Usuario
            {
                Id = solicitanteId,
                Nome = "Gestor",
                Email = "gestor@test.com",
                SenhaHash = "hash"
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(solicitanteId)).ReturnsAsync(usuario);

            var resp1 = new Usuario { Id = 10, Nome = "Colaborador 1", Email = "c1@test.com", SenhaHash = "h" };
            var resp2 = new Usuario { Id = 20, Nome = "Colaborador 2", Email = "c2@test.com", SenhaHash = "h" };

            var tarefas = new List<Tarefa>
            {
                new()
                {
                    Id = 1,
                    Titulo = "T1",
                    DataVencimento = DateTime.UtcNow,
                    CriadorId = solicitanteId,
                    Criador = usuario,
                    Status = Status.Concluida,
                    ResponsaveisTarefas = new List<ResponsavelTarefa> { new() { TarefaId = 1, UsuarioId = 10, Usuario = resp1 } },
                    Respostas = new List<RespostaTarefa> { new() { Id = 1, TarefaId = 1, UsuarioId = 10, Nota = 90 } }
                },
                new()
                {
                    Id = 2,
                    Titulo = "T2",
                    DataVencimento = DateTime.UtcNow,
                    CriadorId = solicitanteId,
                    Criador = usuario,
                    Status = Status.Concluida,
                    ResponsaveisTarefas = new List<ResponsavelTarefa> { new() { TarefaId = 2, UsuarioId = 20, Usuario = resp2 } },
                    Respostas = new List<RespostaTarefa> { new() { Id = 2, TarefaId = 2, UsuarioId = 20, Nota = 80 } }
                }
            };

            var filtro = new FiltroRelatorioDTO { CriadorId = solicitanteId };
            _relatorioRepoMock.Setup(r => r.ObterTarefasParaRelatorioAsync(solicitanteId, false, filtro))
                .ReturnsAsync(tarefas);

            // Act
            var relatorio = await _service.GerarRelatorioTarefasAsync(filtro, solicitanteId);

            // Assert
            Assert.Equal(85.0, relatorio.MediaNotas);
            Assert.Equal(2, relatorio.DesempenhoResponsaveis.Count);
            var d1 = relatorio.DesempenhoResponsaveis.First(d => d.UsuarioId == 10);
            Assert.Equal(90.0, d1.MediaNotas);
            Assert.Equal(1, d1.Concluidas);
            Assert.Equal(100.0, d1.TaxaConclusaoPercentual);
        }

        [Fact]
        public async Task GerarRelatorioEquipe_CalculaMediaNotas_PorMembroEDaEquipe()
        {
            // Arrange
            var equipeId = 5;
            var solicitanteId = 1;
            var usuario1 = new Usuario { Id = 1, Nome = "Admin", Email = "admin@test.com", SenhaHash = "h" };
            var usuario2 = new Usuario { Id = 2, Nome = "Membro 2", Email = "m2@test.com", SenhaHash = "h" };

            var equipe = new Equipe
            {
                Id = equipeId,
                Nome = "Dev Team",
                DataCriacao = DateTime.UtcNow,
                Membros = new List<MembroEquipe>
                {
                    new() { EquipeId = equipeId, UsuarioId = 1, Usuario = usuario1, Cargo = Cargo.Dono },
                    new() { EquipeId = equipeId, UsuarioId = 2, Usuario = usuario2, Cargo = Cargo.Membro }
                }
            };

            _equipeRepoMock.Setup(r => r.ObterEquipePorIdAsync(equipeId)).ReturnsAsync(equipe);
            _authServiceMock.Setup(a => a.ExigirAdminOuDonoDaEquipeAsync(solicitanteId, equipeId)).Returns(Task.CompletedTask);

            var tarefas = new List<Tarefa>
            {
                new()
                {
                    Id = 10,
                    Titulo = "T10",
                    Status = Status.Concluida,
                    DataVencimento = DateTime.UtcNow,
                    CriadorId = 1,
                    Criador = usuario1,
                    ResponsaveisTarefas = new List<ResponsavelTarefa> { new() { TarefaId = 10, UsuarioId = 2, Usuario = usuario2 } },
                    Respostas = new List<RespostaTarefa> { new() { Id = 10, TarefaId = 10, UsuarioId = 2, Nota = 95 } }
                }
            };

            var filtro = new FiltroRelatorioDTO();
            _relatorioRepoMock.Setup(r => r.ObterTarefasDaEquipeAsync(equipeId, filtro))
                .ReturnsAsync(tarefas);

            // Act
            var relatorio = await _service.GerarRelatorioEquipeAsync(equipeId, filtro, solicitanteId);

            // Assert
            Assert.Equal(95.0, relatorio.MetricasGerais.MediaNotas);
            var membro2 = relatorio.DesempenhoMembros.First(m => m.UsuarioId == 2);
            Assert.Equal(95.0, membro2.MediaNotas);
        }
    }
}
