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
    public class UsuarioServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<IArquivoValidacaoService> _arquivoValidacaoServiceMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<ILogger<UsuarioService>> _loggerMock;
        private readonly UsuarioService _usuarioService;

        public UsuarioServiceTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _arquivoValidacaoServiceMock = new Mock<IArquivoValidacaoService>();
            _envMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<UsuarioService>>();

            _usuarioService = new UsuarioService(
                _usuarioRepoMock.Object,
                _passwordServiceMock.Object,
                _arquivoValidacaoServiceMock.Object,
                _envMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task CriarUsuario_RN01_DeveLancarExcecaoSeEmailJaCadastrado()
        {
            // Arrange
            var dto = new CriarUsuarioDTO
            {
                Nome = "Usuario Existente",
                Email = "existente@exemplo.com",
                Senha = "SenhaForte123!"
            };

            _usuarioRepoMock
                .Setup(r => r.ObterUsuarioPorEmailAsync(dto.Email))
                .ReturnsAsync(new Usuario
                {
                    Id = 1,
                    Nome = "Existente",
                    Email = dto.Email,
                    SenhaHash = "hash"
                });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _usuarioService.CriarUsuarioAsync(dto));

            Assert.Equal("E-mail já cadastrado.", ex.Message);
        }

        [Fact]
        public async Task CriarUsuario_RN02_DeveLancarExcecaoSeSenhaForFraca()
        {
            // Arrange
            var dto = new CriarUsuarioDTO
            {
                Nome = "Usuario Novo",
                Email = "novo@exemplo.com",
                Senha = "123"
            };

            _usuarioRepoMock
                .Setup(r => r.ObterUsuarioPorEmailAsync(dto.Email))
                .ReturnsAsync((Usuario?)null);

            _passwordServiceMock
                .Setup(p => p.ValidatePassword(dto.Senha))
                .Returns((false, "A senha deve conter no mínimo 8 caracteres."));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _usuarioService.CriarUsuarioAsync(dto));

            Assert.Contains("8 caracteres", ex.Message);
        }

        [Fact]
        public async Task DeletarUsuario_RN05_DeveBloquearSeUsuarioTiverTarefasEmAndamento()
        {
            // Arrange
            var userId = 5;
            _usuarioRepoMock
                .Setup(r => r.ObterUsuarioPorIdAsync(userId))
                .ReturnsAsync(new Usuario
                {
                    Id = userId,
                    Nome = "Com Tarefas",
                    Email = "tarefas@exemplo.com",
                    SenhaHash = "hash"
                });

            _usuarioRepoMock
                .Setup(r => r.UsuarioTemTarefasEmAndamentoAsync(userId))
                .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _usuarioService.DeletarUsuarioAsync(userId));

            Assert.Contains("tarefas em andamento", ex.Message);
        }
    }
}
