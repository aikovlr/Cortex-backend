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
                _usuarioService.DeletarUsuarioAsync(userId, userId));

            Assert.Contains("tarefas em andamento", ex.Message);
        }

        [Fact]
        public async Task DeletarUsuario_Sucesso_DeveRemoverERetornarTrue()
        {
            // Arrange
            var userId = 5;
            var solicitanteId = 1;
            _usuarioRepoMock
                .Setup(r => r.ObterUsuarioPorIdAsync(userId))
                .ReturnsAsync(new Usuario
                {
                    Id = userId,
                    Nome = "Sem Tarefas",
                    Email = "semtarefas@exemplo.com",
                    SenhaHash = "hash"
                });

            _usuarioRepoMock
                .Setup(r => r.UsuarioTemTarefasEmAndamentoAsync(userId))
                .ReturnsAsync(false);
            _usuarioRepoMock
                .Setup(r => r.UsuarioEhResponsavelPorTarefaEmAndamentoAsync(userId))
                .ReturnsAsync(false);
            _usuarioRepoMock
                .Setup(r => r.RemoverUsuarioAsync(userId))
                .ReturnsAsync(true);

            // Act
            var resultado = await _usuarioService.DeletarUsuarioAsync(userId, solicitanteId);

            // Assert
            Assert.True(resultado);
            _usuarioRepoMock.Verify(r => r.RemoverUsuarioAsync(userId), Times.Once);
        }

        [Fact]
        public async Task AlterarSenhaAsync_NovaSenhaIgualAtual_DeveLancarArgumentException()
        {
            // Arrange
            var userId = 10;
            var usuario = new Usuario
            {
                Id = userId,
                Nome = "Usuario Teste",
                Email = "teste@exemplo.com",
                SenhaHash = "hashAtual"
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(userId)).ReturnsAsync(usuario);
            _passwordServiceMock.Setup(p => p.VerifyPassword("SenhaAtual123!", "hashAtual")).Returns(true);
            _passwordServiceMock.Setup(p => p.VerifyPassword("SenhaAtual123!", "hashAtual")).Returns(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _usuarioService.AlterarSenhaAsync(userId, "SenhaAtual123!", "SenhaAtual123!"));

            Assert.Contains("não pode ser igual", ex.Message);
        }

        [Fact]
        public async Task RedefinirSenhaComCodigoAsync_CodigoValido_DeveAtualizarSenhaEConfirmarEmail()
        {
            // Arrange
            var email = "recuperar@exemplo.com";
            var codigo = "654321";
            var novaSenha = "NovaSenhaForte123!";
            var hashCodigo = BCrypt.Net.BCrypt.HashPassword(codigo);

            var usuario = new Usuario
            {
                Id = 15,
                Nome = "Usuario Recuperacao",
                Email = email,
                SenhaHash = "hashSenhaAntiga",
                EmailVerificado = false,
                CodigoVerificacaoHash = hashCodigo,
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(10)
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(email)).ReturnsAsync(usuario);
            _passwordServiceMock.Setup(p => p.VerifyPassword(novaSenha, "hashSenhaAntiga")).Returns(false);
            _passwordServiceMock.Setup(p => p.ValidatePassword(novaSenha)).Returns((true, string.Empty));
            _passwordServiceMock.Setup(p => p.HashPassword(novaSenha)).Returns("novoHashSenha");

            // Act
            var resultado = await _usuarioService.RedefinirSenhaComCodigoAsync(email, codigo, novaSenha);

            // Assert
            Assert.True(resultado);
            Assert.Equal("novoHashSenha", usuario.SenhaHash);
            Assert.Null(usuario.CodigoVerificacaoHash);
            Assert.Null(usuario.CodigoVerificacaoExpiracao);
            Assert.True(usuario.EmailVerificado);
            _usuarioRepoMock.Verify(r => r.AtualizarUsuarioAsync(usuario), Times.Once);
        }

        [Fact]
        public async Task RedefinirSenhaComCodigoAsync_CodigoInvalido_DeveLancarArgumentException()
        {
            // Arrange
            var email = "recuperar@exemplo.com";
            var usuario = new Usuario
            {
                Id = 15,
                Nome = "Usuario Recuperacao",
                Email = email,
                SenhaHash = "hashSenhaAntiga",
                CodigoVerificacaoHash = BCrypt.Net.BCrypt.HashPassword("111111"),
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(10)
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(email)).ReturnsAsync(usuario);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _usuarioService.RedefinirSenhaComCodigoAsync(email, "999999", "NovaSenha123!"));

            Assert.Equal("Código de recuperação inválido.", ex.Message);
        }

        [Fact]
        public async Task RemoverFotoPerfilAsync_QuandoUsuarioPossuiFoto_DeveRemoverFotoEAtualizar()
        {
            // Arrange
            var anexoMock = new Mock<IAnexoRepository>();
            var serviceComAnexo = new UsuarioService(
                _usuarioRepoMock.Object,
                _passwordServiceMock.Object,
                _arquivoValidacaoServiceMock.Object,
                _envMock.Object,
                _loggerMock.Object,
                anexoMock.Object);

            var usuario = new Usuario
            {
                Id = 20,
                Nome = "Usuario Foto",
                Email = "foto@exemplo.com",
                SenhaHash = "hash123",
                FotoPerfil = new Anexo
                {
                    Id = 55,
                    NomeOriginal = "avatar.png",
                    UrlCaminho = "/uploads/usuarios/avatar.png",
                    MimeType = "image/png"
                }
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(20)).ReturnsAsync(usuario);

            // Act
            var resultado = await serviceComAnexo.RemoverFotoPerfilAsync(20);

            // Assert
            Assert.True(resultado);
            Assert.Null(usuario.FotoPerfil);
            anexoMock.Verify(a => a.RemoverAnexoAsync(55), Times.Once);
            _usuarioRepoMock.Verify(r => r.AtualizarUsuarioAsync(usuario), Times.Once);
        }
    }
}
