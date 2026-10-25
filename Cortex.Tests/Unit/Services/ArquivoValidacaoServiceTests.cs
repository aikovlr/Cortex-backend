using System.Text;
using Cortex.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class ArquivoValidacaoServiceTests
    {
        private readonly ArquivoValidacaoService _service;

        public ArquivoValidacaoServiceTests()
        {
            _service = new ArquivoValidacaoService();
        }

        private static IFormFile CriarMockFormFile(string fileName, byte[] content, string contentType = "application/octet-stream")
        {
            var stream = new MemoryStream(content);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(content.Length);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            return fileMock.Object;
        }

        [Fact]
        public void ValidarImagemPerfil_ImagemJpegValida_DevePassar()
        {
            // Magic bytes JPEG: FF D8 FF
            var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
            var file = CriarMockFormFile("foto.jpg", content, "image/jpeg");

            // Act & Assert (não deve lançar exceção)
            _service.ValidarImagemPerfil(file);
        }

        [Fact]
        public void ValidarImagemPerfil_ExtensaoProibida_DeveLancarArgumentException()
        {
            var content = Encoding.UTF8.GetBytes("alert('xss');");
            var file = CriarMockFormFile("script.exe", content, "application/x-msdownload");

            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidarImagemPerfil(file));

            Assert.Contains("Tipo de arquivo não permitido", ex.Message);
        }

        [Fact]
        public void ValidarImagemPerfil_TamanhoExcedido_DeveLancarArgumentException()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("foto.png");
            fileMock.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB > 5MB

            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidarImagemPerfil(fileMock.Object));

            Assert.Contains("excede o tamanho máximo", ex.Message);
        }

        [Fact]
        public void ValidarImagemPerfil_MagicBytesDivergentes_DeveLancarArgumentException()
        {
            // Extensão .png mas conteúdo não é PNG
            var content = Encoding.UTF8.GetBytes("fake png content");
            var file = CriarMockFormFile("foto.png", content, "image/png");

            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidarImagemPerfil(file));

            Assert.Contains("conteúdo do arquivo não corresponde", ex.Message);
        }

        [Fact]
        public void ValidarArquivo_ArquivoNuloOuVazio_DeveLancarArgumentException()
        {
            var file = CriarMockFormFile("vazio.txt", Array.Empty<byte>());

            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidarArquivo(file, [".txt"], 1024));

            Assert.Contains("inválido ou está vazio", ex.Message);
        }
    }
}
