using Cortex.DTOs;
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
    public class EsqueciSenhaServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<IArquivoValidacaoService> _arquivoValidacaoMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<ILogger<UsuarioService>> _loggerMock;
        private readonly Mock<INotificacaoQueue> _queueMock;
        private readonly Mock<IEmailTemplateService> _templateMock;
        private readonly UsuarioService _usuarioService;

        public EsqueciSenhaServiceTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _arquivoValidacaoMock = new Mock<IArquivoValidacaoService>();
            _envMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<UsuarioService>>();
            _queueMock = new Mock<INotificacaoQueue>();
            _templateMock = new Mock<IEmailTemplateService>();

            _usuarioService = new UsuarioService(
                _usuarioRepoMock.Object,
                _passwordServiceMock.Object,
                _arquivoValidacaoMock.Object,
                _envMock.Object,
                _loggerMock.Object,
                null,
                _queueMock.Object,
                _templateMock.Object);
        }

        [Fact]
        public async Task SolicitarRedefinicao_ComEmailInexistente_DeveRetornarTrueSemLancarExcecao()
        {
            // Arrange
            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync("fantasma@cortex.com")).ReturnsAsync((Usuario?)null);

            // Act
            var resultado = await _usuarioService.SolicitarRedefinicaoSenhaAsync("fantasma@cortex.com");

            // Assert
            Assert.True(resultado);
            _queueMock.Verify(q => q.EnfileirarAsync(It.IsAny<NotificacaoDespacho>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SolicitarRedefinicao_ComEmailExistente_DeveGerarCodigoHashEEnfileirarEmail()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 1,
                Nome = "Ana Silva",
                Email = "ana@cortex.com",
                SenhaHash = "hashAtual"
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(user.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashDoCodigo");
            _templateMock.Setup(t => t.GerarCodigoRedefinicaoSenha(user.Email, user.Nome, It.IsAny<string>(), 15))
                .Returns(new EmailMensagem
                {
                    Destinatario = user.Email,
                    Assunto = "Redefinição de Senha",
                    CorpoHtml = "<p>Html</p>",
                    CorpoTexto = "Texto"
                });

            // Act
            var resultado = await _usuarioService.SolicitarRedefinicaoSenhaAsync(user.Email);

            // Assert
            Assert.True(resultado);
            Assert.Equal("hashDoCodigo", user.CodigoRedefinicaoHash);
            Assert.NotNull(user.CodigoRedefinicaoExpiracao);
            Assert.Equal(0, user.TentativasRedefinicao);
            Assert.NotNull(user.UltimaSolicitacaoRedefinicaoEm);

            _usuarioRepoMock.Verify(r => r.AtualizarUsuarioAsync(user), Times.Once);
            _queueMock.Verify(q => q.EnfileirarAsync(It.Is<NotificacaoDespacho>(d =>
                d.Destinatario == user.Email && d.TipoEvento == "redefinicao_senha"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SolicitarRedefinicao_EmCooldownDe60s_NaoDeveReenviarNemAtualizar()
        {
            // Arrange: Última solicitação feita há 20 segundos
            var user = new Usuario
            {
                Id = 2,
                Nome = "Bruno",
                Email = "bruno@cortex.com",
                SenhaHash = "hashAtual",
                CodigoRedefinicaoHash = "hashOriginal",
                UltimaSolicitacaoRedefinicaoEm = DateTime.UtcNow.AddSeconds(-20)
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(user.Email)).ReturnsAsync(user);

            // Act
            var resultado = await _usuarioService.SolicitarRedefinicaoSenhaAsync(user.Email);

            // Assert: Retorna true mas não reenvia nem altera o hash
            Assert.True(resultado);
            Assert.Equal("hashOriginal", user.CodigoRedefinicaoHash);
            _queueMock.Verify(q => q.EnfileirarAsync(It.IsAny<NotificacaoDespacho>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RedefinirSenha_CodigoVerificacaoNaoServeParaRedefinicao()
        {
            // Arrange: Usuário possui apenas CodigoVerificacaoHash (código de ativação de conta), não CodigoRedefinicaoHash
            var user = new Usuario
            {
                Id = 3,
                Nome = "Carlos",
                Email = "carlos@cortex.com",
                SenhaHash = "hashAtual",
                CodigoVerificacaoHash = "hashAtivacaoConta",
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(10),
                CodigoRedefinicaoHash = null,
                CodigoRedefinicaoExpiracao = null
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(user.Email)).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _usuarioService.RedefinirSenhaComCodigoAsync(user.Email, "123456", "NovaSenha@123"));

            Assert.Equal("Código inválido ou expirado.", ex.Message);
        }

        [Fact]
        public async Task RedefinirSenha_SenhaViolaRN02_DeveRejeitarSemConsumirTentativa()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 4,
                Nome = "Daniel",
                Email = "daniel@cortex.com",
                SenhaHash = "hashAtual",
                CodigoRedefinicaoHash = "hashCodigo",
                CodigoRedefinicaoExpiracao = DateTime.UtcNow.AddMinutes(10),
                TentativasRedefinicao = 0
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(user.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.VerifyPassword("curta", "hashAtual")).Returns(false);
            _passwordServiceMock.Setup(p => p.ValidatePassword("curta")).Returns((false, "A senha deve ter no mínimo 8 caracteres."));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _usuarioService.RedefinirSenhaComCodigoAsync(user.Email, "123456", "curta"));

            Assert.Equal("A senha deve ter no mínimo 8 caracteres.", ex.Message);
            Assert.Equal(0, user.TentativasRedefinicao); // Não consumiu a tentativa!
        }

        [Fact]
        public async Task RedefinirSenha_Apos5TentativasErradas_DeveTravarEInvalidarCodigo()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 5,
                Nome = "Eduardo",
                Email = "eduardo@cortex.com",
                SenhaHash = "hashAtual",
                CodigoRedefinicaoHash = "hashCodigoValido",
                CodigoRedefinicaoExpiracao = DateTime.UtcNow.AddMinutes(10),
                TentativasRedefinicao = 4 // Já errou 4 vezes
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(user.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.VerifyPassword("NovaSenhaForte@123", "hashAtual")).Returns(false);
            _passwordServiceMock.Setup(p => p.ValidatePassword("NovaSenhaForte@123")).Returns((true, string.Empty));
            _passwordServiceMock.Setup(p => p.VerifyPassword("codigoErrado", "hashCodigoValido")).Returns(false);

            // Act: 5ª tentativa errada
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _usuarioService.RedefinirSenhaComCodigoAsync(user.Email, "codigoErrado", "NovaSenhaForte@123"));

            // Assert
            Assert.Equal("Código inválido ou expirado.", ex.Message);
            Assert.Null(user.CodigoRedefinicaoHash); // Código invalidado!
            Assert.Null(user.CodigoRedefinicaoExpiracao);
        }

        [Fact]
        public async Task RedefinirSenha_ComSucesso_AtualizaCamposSessaoEmailVerificadoEEnviaConfirmacao()
        {
            // Arrange
            var user = new Usuario
            {
                Id = 6,
                Nome = "Fabiana",
                Email = "fabiana@cortex.com",
                SenhaHash = "hashAtual",
                EmailVerificado = false,
                CodigoRedefinicaoHash = "hashCodigoValido",
                CodigoRedefinicaoExpiracao = DateTime.UtcNow.AddMinutes(10),
                TentativasRedefinicao = 1
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync(user.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(p => p.VerifyPassword("NovaSenhaValida@123", "hashAtual")).Returns(false);
            _passwordServiceMock.Setup(p => p.ValidatePassword("NovaSenhaValida@123")).Returns((true, string.Empty));
            _passwordServiceMock.Setup(p => p.VerifyPassword("654321", "hashCodigoValido")).Returns(true);
            _passwordServiceMock.Setup(p => p.HashPassword("NovaSenhaValida@123")).Returns("novoHashSenha");
            _templateMock.Setup(t => t.GerarConfirmacaoSenhaAlterada(user.Email, user.Nome))
                .Returns(new EmailMensagem
                {
                    Destinatario = user.Email,
                    Assunto = "Senha alterada",
                    CorpoHtml = "<p>Html</p>",
                    CorpoTexto = "Texto"
                });

            // Act
            var resultado = await _usuarioService.RedefinirSenhaComCodigoAsync(user.Email, "654321", "NovaSenhaValida@123");

            // Assert
            Assert.True(resultado);
            Assert.Equal("novoHashSenha", user.SenhaHash);
            Assert.Null(user.CodigoRedefinicaoHash);
            Assert.Null(user.CodigoRedefinicaoExpiracao);
            Assert.Equal(0, user.TentativasRedefinicao);
            Assert.NotNull(user.SenhaAlteradaEm);
            Assert.True(user.EmailVerificado); // Posse do e-mail comprovada

            _usuarioRepoMock.Verify(r => r.AtualizarUsuarioAsync(user), Times.Once);
            _queueMock.Verify(q => q.EnfileirarAsync(It.Is<NotificacaoDespacho>(d =>
                d.Destinatario == user.Email && d.TipoEvento == "senha_alterada"), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

