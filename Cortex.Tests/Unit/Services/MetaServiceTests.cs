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
                Concluida = true,
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa { TarefaId = dto.TarefaId, Tarefa = tarefa }
                }
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
                UsuarioId = donoDaMetaId
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
            var meta = new Meta { Id = 10, Nome = "Meta Teste", Concluida = false };
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
            var meta = new Meta { Id = 10, Nome = "Meta Teste", Concluida = false };
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
                UsuarioId = solicitanteId,
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
                UsuarioId = 1
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
                Concluida = false
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
                UsuarioId = solicitanteId
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
                UsuarioId = solicitanteId
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
                new Meta { Id = 1, Nome = "Meta 1", UsuarioId = 1 }
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

        [Fact]
        public void IncrementarXpEAtualizarNivel_LimiteAtingido_DeveSubirNivelEManterExcedente()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Gamer",
                Email = "gamer@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 0
            };

            // Act - Nível 1 limite é 100 XP. Ganha 120 XP.
            MetaService.IncrementarXpEAtualizarNivel(usuario, 120);

            // Assert
            Assert.Equal(2, usuario.Nivel);
            Assert.Equal(20, usuario.Experiencia);
        }

        [Fact]
        public void IncrementarXpEAtualizarNivel_XpExatoDoLimite_DeveSubirNivelComZeroXp()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Gamer",
                Email = "gamer@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 0
            };

            // Act - Nível 1 limite é 100 XP. Ganha exatamente 100 XP.
            MetaService.IncrementarXpEAtualizarNivel(usuario, 100);

            // Assert
            Assert.Equal(2, usuario.Nivel);
            Assert.Equal(0, usuario.Experiencia);
        }

        [Fact]
        public void IncrementarXpEAtualizarNivel_XpInsuficiente_NaoDeveSubirNivel()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Gamer",
                Email = "gamer@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 30
            };

            // Act - Ganha 50 XP (total 80 XP < 100)
            MetaService.IncrementarXpEAtualizarNivel(usuario, 50);

            // Assert
            Assert.Equal(1, usuario.Nivel);
            Assert.Equal(80, usuario.Experiencia);
        }

        [Fact]
        public void IncrementarXpEAtualizarNivel_MultiplosNiveis_DeveAumentarNiveisSucessivosEManterExcedente()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Gamer Pro",
                Email = "gamerpro@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 0
            };

            // Act - Ganha 350 XP:
            // Nível 1: precisa de 100 XP -> Nível 2 (restam 250 XP)
            // Nível 2: precisa de 200 XP -> Nível 3 (restam 50 XP)
            // Nível 3: precisa de 300 XP -> 50 < 300 (para no Nível 3 com 50 XP)
            MetaService.IncrementarXpEAtualizarNivel(usuario, 350);

            // Assert
            Assert.Equal(3, usuario.Nivel);
            Assert.Equal(50, usuario.Experiencia);
        }

        [Fact]
        public async Task VerificarConclusaoMeta_MetaDeEquipeConcluida_DeveIncrementarXpEAtualizarNivelDoUsuario()
        {
            // Arrange
            var tarefaId = 10;
            var usuarioId = 5;
            var metaId = 42;
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nome = "Membro Equipe",
                Email = "membro@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 20
            };

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta com Equipe",
                UsuarioId = usuarioId,
                Usuario = usuario,
                Concluida = false,
                MetasEquipes = new List<MetaEquipe>
                {
                    new MetaEquipe
                    {
                        Id = 1,
                        MetaId = metaId,
                        EquipeId = 2,
                        XpRecompensa = 150
                    }
                }
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
                    Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c1@test.com", SenhaHash = "hash" }
                }
            };

            _metaRepoMock.Setup(r => r.ObterMetasPorTarefaIdAsync(tarefaId))
                .ReturnsAsync(new List<Meta> { meta });
            _metaRepoMock.Setup(r => r.ObterTarefasDaMetaAsync(metaId))
                .ReturnsAsync(tarefas);
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(usuarioId))
                .ReturnsAsync(usuario);
            _usuarioRepoMock.Setup(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()))
                .ReturnsAsync((Usuario u) => u);

            // Act
            var concluidas = await _metaService.VerificarConclusaoMetaAsync(tarefaId);

            // Assert
            Assert.Equal(1, concluidas);
            Assert.True(meta.Concluida);
            // 20 + 150 = 170 XP. Nível 1 consome 100 -> Nível 2, saldo 70 XP
            Assert.Equal(2, usuario.Nivel);
            Assert.Equal(70, usuario.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.Is<Usuario>(u => u.Id == usuarioId && u.Nivel == 2 && u.Experiencia == 70)), Times.Once);
        }

        [Fact]
        public async Task VerificarConclusaoMeta_MetaPuramenteIndividual_NaoDeveAcumularXp()
        {
            // Arrange
            var tarefaId = 10;
            var usuarioId = 5;
            var metaId = 42;
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nome = "Individual",
                Email = "ind@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 20
            };

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta Individual",
                UsuarioId = usuarioId,
                Usuario = usuario,
                Concluida = false,
                MetasEquipes = new List<MetaEquipe>() // Vazio -> puramente individual
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
                    Criador = new Usuario { Id = 1, Nome = "Criador", Email = "c1@test.com", SenhaHash = "hash" }
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
            // Metas puramente individuais não devem alterar XP nem Nível
            Assert.Equal(1, usuario.Nivel);
            Assert.Equal(20, usuario.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task CriarMeta_MetaDeEquipeJaConcluida_DeveIncrementarXp()
        {
            // Arrange
            var solicitanteId = 1;
            var usuarioAlvo = new Usuario
            {
                Id = solicitanteId,
                Nome = "Criador",
                Email = "c@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 0
            };

            var tarefa = new Tarefa
            {
                Id = 10,
                Titulo = "Tarefa Concluida",
                Status = Status.Concluida,
                DataVencimento = DateTime.UtcNow.AddDays(7),
                CriadorId = solicitanteId,
                Criador = usuarioAlvo
            };

            var dto = new CriarMetaDTO
            {
                Nome = "Meta Equipe Feita",
                TarefaId = 10,
                EquipeId = 2,
                XpRecompensa = 110
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(dto.TarefaId)).ReturnsAsync(tarefa);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(2)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDaEquipeAsync(solicitanteId, 2)).ReturnsAsync(true);
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(solicitanteId)).ReturnsAsync(usuarioAlvo);
            _metaRepoMock.Setup(r => r.AdicionarMetaAsync(It.IsAny<Meta>())).ReturnsAsync((Meta m) => m);

            // Act
            var resultado = await _metaService.CriarMetaAsync(dto, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            Assert.True(resultado.Concluida);
            // 110 XP -> Nível 2, 10 XP
            Assert.Equal(2, usuarioAlvo.Nivel);
            Assert.Equal(10, usuarioAlvo.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(usuarioAlvo), Times.Once);
        }

        [Fact]
        public async Task AtribuirEquipe_ComXpRecompensa_DeveAdicionarMetaEquipeComXpInformado()
        {
            // Arrange
            var metaId = 1;
            var equipeId = 5;
            var solicitanteId = 10;
            var xpRecompensa = 250;

            var meta = new Meta
            {
                Id = metaId,
                Nome = "Meta da Equipe",
                UsuarioId = solicitanteId
            };

            var membroDono = new MembroEquipe
            {
                Id = 1,
                EquipeId = equipeId,
                UsuarioId = solicitanteId,
                Cargo = Cargo.Dono
            };

            _metaRepoMock.Setup(r => r.ObterMetaPorIdAsync(metaId)).ReturnsAsync(meta);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(equipeId)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDaEquipeAsync(solicitanteId, equipeId)).ReturnsAsync(true);
            _metaRepoMock.Setup(r => r.MetaEquipeExisteAsync(metaId, equipeId)).ReturnsAsync(false);
            _membroEquipeRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId)).ReturnsAsync(membroDono);
            _membroEquipeRepoMock.Setup(r => r.ObterMembrosEquipePorEquipeIdAsync(equipeId)).ReturnsAsync(new List<MembroEquipe> { membroDono });

            // Act
            var resultado = await _metaService.AtribuirEquipeAsync(metaId, equipeId, solicitanteId, xpRecompensa);

            // Assert
            Assert.NotNull(resultado);
            _metaRepoMock.Verify(r => r.AdicionarMetaEquipeAsync(It.Is<MetaEquipe>(me =>
                me.MetaId == metaId &&
                me.EquipeId == equipeId &&
                me.XpRecompensa == xpRecompensa)), Times.Once);
        }

        [Fact]
        public void DecrementarXp_XpSuficiente_DeveReduzirSaldo()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Usuario Teste",
                Email = "user@test.com",
                SenhaHash = "hash",
                Nivel = 2,
                Experiencia = 20
            };

            // Act
            MetaService.DecrementarXp(usuario, 5);

            // Assert
            Assert.Equal(15, usuario.Experiencia);
        }

        [Fact]
        public void DecrementarXp_XpInsuficiente_DeveLimitarEmZero()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Usuario Teste",
                Email = "user@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 3
            };

            // Act - Tenta remover 5 XP (maior que o saldo de 3)
            MetaService.DecrementarXp(usuario, 5);

            // Assert - Deve limitar em 0, nunca negativo
            Assert.Equal(0, usuario.Experiencia);
        }

        [Fact]
        public async Task CriarMeta_MetaDeEquipeSemXpExplicito_DeveIniciarEmZeroEAumentar5PorTarefa()
        {
            // Arrange
            var solicitanteId = 1;
            var usuarioAlvo = new Usuario
            {
                Id = solicitanteId,
                Nome = "Criador",
                Email = "c@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 0
            };

            var tarefas = new List<Tarefa>
            {
                new Tarefa { Id = 1, Titulo = "T1", Status = Status.Pendente, DataVencimento = DateTime.UtcNow.AddDays(7), CriadorId = solicitanteId, Criador = usuarioAlvo },
                new Tarefa { Id = 2, Titulo = "T2", Status = Status.Pendente, DataVencimento = DateTime.UtcNow.AddDays(7), CriadorId = solicitanteId, Criador = usuarioAlvo },
                new Tarefa { Id = 3, Titulo = "T3", Status = Status.Pendente, DataVencimento = DateTime.UtcNow.AddDays(7), CriadorId = solicitanteId, Criador = usuarioAlvo }
            };

            var dto = new CriarMetaDTO
            {
                Nome = "Meta 3 Tarefas",
                TarefaIds = new List<int> { 1, 2, 3 },
                EquipeId = 2,
                XpRecompensa = null // Não informado: deve iniciar em 0 e somar 5 por tarefa vinculada (3 * 5 = 15)
            };

            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(1)).ReturnsAsync(tarefas[0]);
            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(2)).ReturnsAsync(tarefas[1]);
            _tarefaRepoMock.Setup(r => r.ObterTarefaPorIdAsync(3)).ReturnsAsync(tarefas[2]);
            _equipeRepoMock.Setup(r => r.EquipeExisteAsync(2)).ReturnsAsync(true);
            _authServiceMock.Setup(a => a.EhAdminOuDonoDaEquipeAsync(solicitanteId, 2)).ReturnsAsync(true);
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(solicitanteId)).ReturnsAsync(usuarioAlvo);
            _metaRepoMock.Setup(r => r.AdicionarMetaAsync(It.IsAny<Meta>())).ReturnsAsync((Meta m) => m);

            // Act
            var resultado = await _metaService.CriarMetaAsync(dto, solicitanteId);

            // Assert
            Assert.NotNull(resultado);
            _metaRepoMock.Verify(r => r.AdicionarMetaAsync(It.Is<Meta>(m =>
                m.MetasEquipes.Any(me => me.EquipeId == 2 && me.XpRecompensa == 15))), Times.Once);
        }

        [Fact]
        public async Task VerificarMetasExpiradas_MetasNaoConcluidasComPrazoExpirado_DeveContabilizarERemover5XpPorMeta()
        {
            // Arrange
            var usuarioId = 10;
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nome = "Aluno",
                Email = "aluno@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 25
            };

            var metaExpirada1 = new Meta
            {
                Id = 101,
                Nome = "Meta Expirada 1",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddDays(-2),
                PenalidadeAplicada = false,
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa
                    {
                        TarefaId = 1,
                        Tarefa = new Tarefa
                        {
                            Id = 1,
                            Titulo = "T1",
                            Status = Status.Pendente,
                            DataVencimento = DateTime.UtcNow.AddDays(-3),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    }
                },
                MetasEquipes = new List<MetaEquipe>
                {
                    new MetaEquipe { EquipeId = 1, XpRecompensa = 5 }
                }
            };

            var metaExpirada2 = new Meta
            {
                Id = 102,
                Nome = "Meta Expirada 2",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddDays(-1),
                PenalidadeAplicada = false,
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa
                    {
                        TarefaId = 2,
                        Tarefa = new Tarefa
                        {
                            Id = 2,
                            Titulo = "T2",
                            Status = Status.Atrasada,
                            DataVencimento = DateTime.UtcNow.AddDays(-2),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    }
                },
                MetasEquipes = new List<MetaEquipe>
                {
                    new MetaEquipe { EquipeId = 1, XpRecompensa = 5 }
                }
            };

            _metaRepoMock.Setup(r => r.ObterTodasMetasAsync())
                .ReturnsAsync(new List<Meta> { metaExpirada1, metaExpirada2 });
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(usuarioId))
                .ReturnsAsync(usuario);
            _usuarioRepoMock.Setup(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()))
                .ReturnsAsync((Usuario u) => u);
            _metaRepoMock.Setup(r => r.AtualizarMetaAsync(It.IsAny<Meta>()))
                .ReturnsAsync((Meta m) => m);

            // Act - 2 metas expiradas não concluídas -> Deve remover 10 XP (2 * 5)
            var totalPenalizadas = await _metaService.VerificarMetasExpiradasAsync();

            // Assert
            Assert.Equal(2, totalPenalizadas);
            Assert.True(metaExpirada1.PenalidadeAplicada);
            Assert.True(metaExpirada2.PenalidadeAplicada);
            // Saldo inicial 25 - (2 * 5) = 15 XP
            Assert.Equal(15, usuario.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.Is<Usuario>(u => u.Experiencia == 15)), Times.Once);
        }

        [Fact]
        public async Task VerificarMetasExpiradas_TarefasConcluidasEmMetaExpirada_NaoDevemAlterarSaldoXp()
        {
            // Arrange
            var usuarioId = 10;
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nome = "Aluno",
                Email = "aluno@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 10
            };

            // Meta com 2 tarefas concluídas e 1 pendente (meta NÃO está concluída)
            var metaComTarefasMistas = new Meta
            {
                Id = 201,
                Nome = "Meta Parcial Expirada",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddHours(-3),
                PenalidadeAplicada = false,
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa
                    {
                        TarefaId = 1,
                        Tarefa = new Tarefa
                        {
                            Id = 1,
                            Titulo = "T1",
                            Status = Status.Concluida,
                            DataVencimento = DateTime.UtcNow.AddHours(-4),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    },
                    new MetaTarefa
                    {
                        TarefaId = 2,
                        Tarefa = new Tarefa
                        {
                            Id = 2,
                            Titulo = "T2",
                            Status = Status.Concluida,
                            DataVencimento = DateTime.UtcNow.AddHours(-4),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    },
                    new MetaTarefa
                    {
                        TarefaId = 3,
                        Tarefa = new Tarefa
                        {
                            Id = 3,
                            Titulo = "T3",
                            Status = Status.Pendente,
                            DataVencimento = DateTime.UtcNow.AddHours(-4),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    }
                },
                MetasEquipes = new List<MetaEquipe>
                {
                    new MetaEquipe { EquipeId = 1, XpRecompensa = 15 }
                }
            };

            _metaRepoMock.Setup(r => r.ObterTodasMetasAsync())
                .ReturnsAsync(new List<Meta> { metaComTarefasMistas });
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(usuarioId))
                .ReturnsAsync(usuario);
            _usuarioRepoMock.Setup(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()))
                .ReturnsAsync((Usuario u) => u);
            _metaRepoMock.Setup(r => r.AtualizarMetaAsync(It.IsAny<Meta>()))
                .ReturnsAsync((Meta m) => m);

            // Act
            var totalPenalizadas = await _metaService.VerificarMetasExpiradasAsync();

            // Assert
            Assert.Equal(1, totalPenalizadas);
            Assert.True(metaComTarefasMistas.PenalidadeAplicada);
            // Saldo deve sofrer penalidade de -5 XP, e as 2 tarefas com status Concluida NÃO devem adicionar XP
            Assert.Equal(5, usuario.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.Is<Usuario>(u => u.Experiencia == 5)), Times.Once);
        }

        [Fact]
        public async Task VerificarMetasExpiradas_MetaJaPenalizada_NaoDevePenalizarNovamente()
        {
            // Arrange
            var usuarioId = 10;
            var metaJaPenalizada = new Meta
            {
                Id = 301,
                Nome = "Meta Já Penalizada",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddDays(-5),
                PenalidadeAplicada = true // Já foi penalizada anteriormente
            };

            _metaRepoMock.Setup(r => r.ObterTodasMetasAsync())
                .ReturnsAsync(new List<Meta> { metaJaPenalizada });

            // Act
            var totalPenalizadas = await _metaService.VerificarMetasExpiradasAsync();

            // Assert - Não deve reprocessar nem penalizar novamente
            Assert.Equal(0, totalPenalizadas);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task VerificarMetasExpiradas_MetaIndividualExpirada_NaoDeveRemoverXpNemEmitirPenalidade()
        {
            // Arrange
            var usuarioId = 10;
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nome = "Aluno",
                Email = "aluno@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 25
            };

            // Meta individual (sem equipes vinculadas)
            var metaIndividualExpirada = new Meta
            {
                Id = 105,
                Nome = "Meta Individual Expirada",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddDays(-2),
                PenalidadeAplicada = false,
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa
                    {
                        TarefaId = 1,
                        Tarefa = new Tarefa
                        {
                            Id = 1,
                            Titulo = "T1",
                            Status = Status.Pendente,
                            DataVencimento = DateTime.UtcNow.AddDays(-3),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    }
                },
                MetasEquipes = new List<MetaEquipe>() // Meta individual
            };

            _metaRepoMock.Setup(r => r.ObterTodasMetasAsync())
                .ReturnsAsync(new List<Meta> { metaIndividualExpirada });
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(usuarioId))
                .ReturnsAsync(usuario);
            _metaRepoMock.Setup(r => r.AtualizarMetaAsync(It.IsAny<Meta>()))
                .ReturnsAsync((Meta m) => m);

            // Act
            var totalPenalizadas = await _metaService.VerificarMetasExpiradasAsync();

            // Assert - Meta individual NÃO deve sofrer dedução de XP
            Assert.Equal(0, totalPenalizadas);
            Assert.True(metaIndividualExpirada.PenalidadeAplicada);
            Assert.Equal(25, usuario.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()), Times.Never);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int?>()),
                Times.Never);
        }

        [Fact]
        public async Task VerificarMetasExpiradas_MetasEquipeEIndividualMistas_DevePenalizarApenasMetasDeEquipe()
        {
            // Arrange
            var usuarioId = 10;
            var usuario = new Usuario
            {
                Id = usuarioId,
                Nome = "Aluno",
                Email = "aluno@test.com",
                SenhaHash = "hash",
                Nivel = 1,
                Experiencia = 25
            };

            var metaEquipe = new Meta
            {
                Id = 101,
                Nome = "Meta de Equipe Expirada",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddDays(-2),
                PenalidadeAplicada = false,
                MetasEquipes = new List<MetaEquipe> { new MetaEquipe { EquipeId = 1, XpRecompensa = 5 } },
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa
                    {
                        TarefaId = 1,
                        Tarefa = new Tarefa
                        {
                            Id = 1,
                            Titulo = "T1",
                            Status = Status.Pendente,
                            DataVencimento = DateTime.UtcNow.AddDays(-3),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    }
                }
            };

            var metaIndividual = new Meta
            {
                Id = 102,
                Nome = "Meta Individual Expirada",
                UsuarioId = usuarioId,
                Concluida = false,
                DataFechamento = DateTime.UtcNow.AddDays(-1),
                PenalidadeAplicada = false,
                MetasEquipes = new List<MetaEquipe>(), // Individual
                MetasTarefas = new List<MetaTarefa>
                {
                    new MetaTarefa
                    {
                        TarefaId = 2,
                        Tarefa = new Tarefa
                        {
                            Id = 2,
                            Titulo = "T2",
                            Status = Status.Atrasada,
                            DataVencimento = DateTime.UtcNow.AddDays(-2),
                            CriadorId = usuarioId,
                            Criador = usuario
                        }
                    }
                }
            };

            _metaRepoMock.Setup(r => r.ObterTodasMetasAsync())
                .ReturnsAsync(new List<Meta> { metaEquipe, metaIndividual });
            _usuarioRepoMock.Setup(u => u.ObterUsuarioPorIdAsync(usuarioId))
                .ReturnsAsync(usuario);
            _usuarioRepoMock.Setup(u => u.AtualizarUsuarioAsync(It.IsAny<Usuario>()))
                .ReturnsAsync((Usuario u) => u);
            _metaRepoMock.Setup(r => r.AtualizarMetaAsync(It.IsAny<Meta>()))
                .ReturnsAsync((Meta m) => m);

            // Act - Apenas a meta de equipe deve descontar 5 XP (saldo: 25 - 5 = 20)
            var totalPenalizadas = await _metaService.VerificarMetasExpiradasAsync();

            // Assert
            Assert.Equal(1, totalPenalizadas);
            Assert.True(metaEquipe.PenalidadeAplicada);
            Assert.True(metaIndividual.PenalidadeAplicada);
            Assert.Equal(20, usuario.Experiencia);
            _usuarioRepoMock.Verify(u => u.AtualizarUsuarioAsync(It.Is<Usuario>(u => u.Experiencia == 20)), Times.Once);
            _notificacaoServiceMock.Verify(n => n.CriarNotificacaoAsync(
                usuarioId,
                "meta",
                "Penalidade de XP: Meta Expirada",
                It.Is<string>(msg => msg.Contains("1 meta(s) de equipe") && msg.Contains("5 XP")),
                "/metas",
                false,
                null), Times.Once);
        }
    }
}
