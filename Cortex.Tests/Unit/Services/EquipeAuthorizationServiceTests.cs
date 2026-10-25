using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Implementations;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class EquipeAuthorizationServiceTests
    {
        private readonly Mock<IMembroEquipeRepository> _membroRepoMock;
        private readonly EquipeAuthorizationService _service;

        public EquipeAuthorizationServiceTests()
        {
            _membroRepoMock = new Mock<IMembroEquipeRepository>();
            _service = new EquipeAuthorizationService(_membroRepoMock.Object);
        }

        [Theory]
        [InlineData(Cargo.Dono, true)]
        [InlineData(Cargo.Admin, true)]
        [InlineData(Cargo.Membro, false)]
        public async Task EhAdminOuDonoDaEquipe_DeveValidarCorretamente(Cargo cargo, bool esperado)
        {
            // Arrange
            var membro = new MembroEquipe { UsuarioId = 1, EquipeId = 10, Cargo = cargo };
            _membroRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(10, 1))
                .ReturnsAsync(membro);

            // Act
            var resultado = await _service.EhAdminOuDonoDaEquipeAsync(1, 10);

            // Assert
            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public async Task EhAdminOuDonoDaEquipe_UsuarioNaoMembro_DeveRetornarFalse()
        {
            // Arrange
            _membroRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(10, 1))
                .ReturnsAsync((MembroEquipe?)null);

            // Act
            var resultado = await _service.EhAdminOuDonoDaEquipeAsync(1, 10);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task EhDonoDaEquipe_ApenasDonoRetornaTrue()
        {
            // Arrange
            var membroAdmin = new MembroEquipe { UsuarioId = 1, EquipeId = 10, Cargo = Cargo.Admin };
            _membroRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(10, 1))
                .ReturnsAsync(membroAdmin);

            // Act
            var ehDono = await _service.EhDonoDaEquipeAsync(1, 10);

            // Assert
            Assert.False(ehDono);
        }

        [Fact]
        public async Task EhAdminOuDonoDaTarefa_SolicitanteAdminNaEquipeDoResponsavel_DeveRetornarTrue()
        {
            // Arrange
            var tarefa = new Tarefa
            {
                Id = 1,
                Titulo = "Tarefa",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                CriadorId = 99,
                Criador = new Usuario { Id = 99, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                Status = Status.Pendente,
                Prioridade = Prioridade.Media,
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new() { TarefaId = 1, UsuarioId = 2 }
                }
            };

            // Responsável (id 2) pertence à Equipe 10
            _membroRepoMock.Setup(r => r.ObterMembrosPorUsuarioEEquipesIdsAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(2))))
                .ReturnsAsync(new List<MembroEquipe>
                {
                    new() { UsuarioId = 2, EquipeId = 10, Cargo = Cargo.Membro }
                });

            // Solicitante (id 1) é Admin na Equipe 10
            _membroRepoMock.Setup(r => r.ObterMembrosPorUsuarioEEquipesAsync(1, It.Is<IEnumerable<int>>(ids => ids.Contains(10))))
                .ReturnsAsync(new List<MembroEquipe>
                {
                    new() { UsuarioId = 1, EquipeId = 10, Cargo = Cargo.Admin }
                });

            // Act
            var resultado = await _service.EhAdminOuDonoDaTarefaAsync(1, tarefa);

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public async Task EhAdminOuDonoDaTarefa_SolicitanteAdminEmOutraEquipeNaoRelacionada_DeveRetornarFalse()
        {
            // Arrange
            var tarefa = new Tarefa
            {
                Id = 1,
                Titulo = "Tarefa",
                DataVencimento = DateTime.UtcNow.AddDays(1),
                CriadorId = 99,
                Criador = new Usuario { Id = 99, Nome = "Criador", Email = "c@test.com", SenhaHash = "hash" },
                Status = Status.Pendente,
                Prioridade = Prioridade.Media,
                ResponsaveisTarefas = new List<ResponsavelTarefa>
                {
                    new() { TarefaId = 1, UsuarioId = 2 }
                }
            };

            // Responsável (id 2) pertence apenas à Equipe 10
            _membroRepoMock.Setup(r => r.ObterMembrosPorUsuarioEEquipesIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<MembroEquipe>
                {
                    new() { UsuarioId = 2, EquipeId = 10, Cargo = Cargo.Membro }
                });

            // Solicitante (id 1) NÃO tem cargo de Dono/Admin na equipe 10 (apenas em equipe 20)
            _membroRepoMock.Setup(r => r.ObterMembrosPorUsuarioEEquipesAsync(1, It.Is<IEnumerable<int>>(ids => ids.Contains(10))))
                .ReturnsAsync(new List<MembroEquipe>()); // Não é membro da equipe 10

            // Act
            var resultado = await _service.EhAdminOuDonoDaTarefaAsync(1, tarefa);

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task ExigirAdminOuDonoDaEquipe_MembroComum_LancaForbiddenException()
        {
            // Arrange
            var membro = new MembroEquipe { UsuarioId = 1, EquipeId = 10, Cargo = Cargo.Membro };
            _membroRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(10, 1))
                .ReturnsAsync(membro);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() => _service.ExigirAdminOuDonoDaEquipeAsync(1, 10));
        }

        [Fact]
        public async Task ExigirDonoDaEquipe_Admin_LancaForbiddenException()
        {
            // Arrange
            var membro = new MembroEquipe { UsuarioId = 1, EquipeId = 10, Cargo = Cargo.Admin };
            _membroRepoMock.Setup(r => r.ObterMembroPorEquipeEUsuarioAsync(10, 1))
                .ReturnsAsync(membro);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() => _service.ExigirDonoDaEquipeAsync(1, 10));
        }
    }
}
