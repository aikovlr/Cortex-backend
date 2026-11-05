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
        private readonly Mock<INotificacaoService> _notificacaoServiceMock;
        private readonly Mock<IEquipeRepository> _equipeRepoMock;
        private readonly Mock<IMembroEquipeRepository> _membroEquipeRepoMock;
        private readonly Mock<ILogger<MetaService>> _loggerMock;
        private readonly MetaService _metaService;

        public MetaServiceTests()
        {
            _metaRepoMock = new Mock<IMetaRepository>();
            _tarefaRepoMock = new Mock<ITarefaRepository>();
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _authServiceMock = new Mock<IEquipeAuthorizationService>();
            _notificacaoServiceMock = new Mock<INotificacaoService>();
            _equipeRepoMock = new Mock<IEquipeRepository>();
            _membroEquipeRepoMock = new Mock<IMembroEquipeRepository>();
            _loggerMock = new Mock<ILogger<MetaService>>();

            _metaService = new MetaService(
                _metaRepoMock.Object,
                _tarefaRepoMock.Object,
                _usuarioRepoMock.Object,
                _authServiceMock.Object,
                _loggerMock.Object,
                _notificacaoServiceMock.Object,
                _equipeRepoMock.Object,
                _membroEquipeRepoMock.Object);
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

        [Fact]
        public async Task VerificarConclusaoMeta_QuandoTodasTarefasConcluidas_DeveNotificarUsuario()
        {
            // Arrange
            var tarefaId = 10;
            var usuarioId = 5;
            var metaId = 42;
            var meta = new Meta
            {
                Id = metaId,
                Nome = "Lançar Produto",
                UsuarioId = usuarioId,
                Concluida = false,
                TarefaId = tarefaId
            };

            var tarefas = new List<Tarefa>
            {
                new Tarefa
                {
                    Id = tarefaId,
                    Titulo = "Tarefa 1",
                    Status = Status.Concluida,
                    DataVencimento = DateTime.UtcNow.AddDays(1),
                    CriadorId = 1,
                    Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" }
                }
            };

            _metaRepoMock.Setup(r => r.ObterMetasPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<Meta> { meta });
            _metaRepoMock.Setup(r => r.ObterTarefasDaMetaAsync(metaId))
                .ReturnsAsync(tarefas);

            // Act
            var concluidas = await _metaService.VerificarConclusaoMetaAsync(tarefaId);

            // Assert
            Assert.Equal(1, concluidas);
            Assert.True(meta.Concluida);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                usuarioId,
                "meta",
                "Meta Concluída",
                It.Is<string>(msg => msg.Contains("Lançar Produto")),
                "/metas",
                false,
                null), Times.Once);
        }

        [Fact]
        public async Task CriarMeta_AtribuirAIntegrante_SucessoQuandoSolicitanteEhAdminOuDono()
        {
            // Arrange
            var solicitanteId = 1;
            var integranteId = 2;
            var tarefa = new Tarefa
            {
                Id = 10,
                Titulo = "Tarefa Base",
                DataVencimento = DateTime.UtcNow.AddDays(7),
                CriadorId = solicitanteId,
                Criador = new Usuario { Id = solicitanteId, Nome = "Admin", Email = "admin@test.com", SenhaHash = "hash" }
            };

            var dto = new CriarMetaDTO
            {
                Nome = "Meta Atribuída",
                TarefaId = 10,
                UsuarioId = integranteId
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(dto.TarefaId)).ReturnsAsync(tarefa);
            _usuarioRepoMock.Setup(r => r.UsuarioExisteAsync(integranteId)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(solicitanteId, integranteId)).ReturnsAsync(true);
            _metaRepoMock.Setup(r => r.AdicionarMetaAsync(It.IsAny<Meta>())).ReturnsAsync((Meta m) => m);

            // Act
            var resultado = await _metaService.CriarMetaAsync(dto, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal("Meta Atribuída", resultado.Nome);
            Assert.Equal(integranteId, resultado.UsuarioId);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                integranteId,
                "meta",
                "Nova Meta Atribuída",
                It.Is<string>(s => s.Contains("Meta Atribuída")),
                "/metas",
                false,
                null), Times.Once);
        }

        [Fact]
        public async Task CriarMeta_AtribuirAIntegrante_DeveLancarForbiddenExceptionQuandoNaoEhAdminOuDono()
        {
            // Arrange
            var solicitanteId = 3;
            var integranteId = 4;
            var tarefa = new Tarefa
            {
                Id = 10,
                Titulo = "Tarefa Base",
                DataVencimento = DateTime.UtcNow.AddDays(7),
                CriadorId = solicitanteId,
                Criador = new Usuario { Id = solicitanteId, Nome = "Membro", Email = "membro@test.com", SenhaHash = "hash" }
            };

            var dto = new CriarMetaDTO
            {
                Nome = "Meta Não Permitida",
                TarefaId = 10,
                UsuarioId = integranteId
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(dto.TarefaId)).ReturnsAsync(tarefa);
            _usuarioRepoMock.Setup(r => r.UsuarioExisteAsync(integranteId)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDeAlgumaEquipeDoUsuarioAsync(solicitanteId, integranteId)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _metaService.CriarMetaAsync(dto, solicitanteId));
        }

        [Fact]
        public async Task AtribuirEquipe_SucessoQuandoSolicitanteEhAdminOuDono()
        {
            // Arrange
            var metaId = 1;
            var equipeId = 5;
            var solicitanteId = 10;

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta da Equipe",
                UsuarioId = solicitanteId,
                TarefaId = 1
            };

            var membros = new List<MembroEquipe>
            {
                new MembroEquipe { Id = 1, EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Dono },
                new MembroEquipe { Id = 2, EquipeId = equipeId, UsuarioId = 20, Cargo = Cargo.Membro }
            };

            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(metaId)).ReturnsAsync(meta);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDaEquipeAsync(solicitanteId, equipeId)).ReturnsAsync(true);
            _metaRepoMock.Setup(r => r.MetaEquipeExisteAsync(metaId, equipeId)).ReturnsAsync(false);
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId)).ReturnsAsync(membros[0]);
            _membroEquipeRepoMock.Setup(r => r.ObterMembrosEquipePorEquipeIdAsync(equipeId)).ReturnsAsync(membros);

            // Act
            var resultado = await _metaService.AtribuirEquipeAsync(metaId, equipeId, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            _metaRepoMock.Verify(r => r.AdicionarMetaEquipeAsync(It.Is<MetaEquipe>(me => me.MetaId == metaId && me.EquipeId == equipeId)), Times.Once);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                20,
                "meta",
                "Nova Meta de Equipe",
                It.Is<string>(s => s.Contains("Meta da Equipe")),
                "/metas",
                false,
                null), Times.Once);
        }

        [Fact]
        public async Task AtribuirEquipe_DeveLancarForbiddenExceptionQuandoNaoEhAdminOuDono()
        {
            // Arrange
            var metaId = 1;
            var equipeId = 5;
            var solicitanteId = 30;

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta da Equipe",
                UsuarioId = solicitanteId,
                TarefaId = 1
            };

            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(metaId)).ReturnsAsync(meta);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDaEquipeAsync(solicitanteId, equipeId)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _metaService.AtribuirEquipeAsync(metaId, equipeId, solicitanteId));
        }

        [Fact]
        public async Task ListarMetasPorEquipe_DeveRetornarMetasVinculadas()
        {
            // Arrange
            var equipeId = 5;
            var solicitanteId = 10;
            var membro = new MembroEquipe { Id = 1, EquipeId = equipeId, UsuarioId = solicitanteId, Cargo = Cargo.Membro };
            var metas = new List<Meta>
            {
                new Meta { Id = 1, Nome = "Meta 1", TarefaId = 10, UsuarioId = 1 }
            };

            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId)).ReturnsAsync(true);
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId)).ReturnsAsync(membro);
            _metaRepoMock.Setup(r => r.ObterMetasPorEquipeIdAsync(equipeId)).ReturnsAsync(metas);

            // Act
            var resultado = await _metaService.ListarMetasPorEquipeAsync(equipeId, solicitanteId);

            // Assert
            Assert.Single(resultado);
            Assert.Equal("Meta 1", resultado.First().Nome);
        }
    }
}
