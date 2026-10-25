using Cortex.Entities;
using Cortex.Repositories.Interface;
using Cortex.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class VerificacaoEmailServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
        private readonly Mock<ILogger<VerificacaoEmailService>> _loggerMock;
        private readonly VerificacaoEmailService _service;

        public VerificacaoEmailServiceTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioRepository>();
            _loggerMock = new Mock<ILogger<VerificacaoEmailService>>();
            _service = new VerificacaoEmailService(_usuarioRepoMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GerarEArmazenarCodigoAsync_DeveGerarCodigoDe6DigitosEArmazenarHash()
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Teste",
                Email = "teste@exemplo.com",
                SenhaHash = "hash"
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorIdAsync(1))
                .ReturnsAsync(usuario);

            var codigo = await _service.GerarEArmazenarCodigoAsync(1);

            Assert.NotNull(codigo);
            Assert.Equal(6, codigo.Length);
            Assert.NotNull(usuario.CodigoVerificacaoHash);
            Assert.NotNull(usuario.CodigoVerificacaoExpiracao);
            Assert.True(BCrypt.Net.BCrypt.Verify(codigo, usuario.CodigoVerificacaoHash));
            _usuarioRepoMock.Verify(r => r.AtualizarUsuarioAsync(usuario), Times.Once);
        }

        [Fact]
        public async Task VerificarCodigoAsync_CodigoCorreto_DeveMarcarEmailComoVerificadoELimparHash()
        {
            const string codigo = "654321";
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Teste",
                Email = "teste@exemplo.com",
                SenhaHash = "hash",
                EmailVerificado = false,
                CodigoVerificacaoHash = BCrypt.Net.BCrypt.HashPassword(codigo),
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(10)
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync("teste@exemplo.com"))
                .ReturnsAsync(usuario);

            var resultado = await _service.VerificarCodigoAsync("teste@exemplo.com", codigo);

            Assert.True(resultado);
            Assert.True(usuario.EmailVerificado);
            Assert.Null(usuario.CodigoVerificacaoHash);
            Assert.Null(usuario.CodigoVerificacaoExpiracao);
            _usuarioRepoMock.Verify(r => r.AtualizarUsuarioAsync(usuario), Times.Once);
        }

        [Fact]
        public async Task VerificarCodigoAsync_CodigoExpirado_DeveLancarInvalidOperationException()
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Teste",
                Email = "teste@exemplo.com",
                SenhaHash = "hash",
                CodigoVerificacaoHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(-1)
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync("teste@exemplo.com"))
                .ReturnsAsync(usuario);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.VerificarCodigoAsync("teste@exemplo.com", "123456"));
        }

        [Fact]
        public async Task VerificarCodigoAsync_CodigoInvalido_DeveLancarArgumentException()
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Teste",
                Email = "teste@exemplo.com",
                SenhaHash = "hash",
                CodigoVerificacaoHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(10)
            };

            _usuarioRepoMock.Setup(r => r.ObterUsuarioPorEmailAsync("teste@exemplo.com"))
                .ReturnsAsync(usuario);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.VerificarCodigoAsync("teste@exemplo.com", "999999"));
        }
    }
}
