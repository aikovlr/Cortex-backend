using Cortex.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cortex.Tests.Unit.Services
{
    public class EmailTemplateServiceTests
    {
        private readonly EmailTemplateService _templateService;

        public EmailTemplateServiceTests()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:FrontendBaseUrl"] = "https://app.cortex.com"
                })
                .Build();

            _templateService = new EmailTemplateService(config);
        }

        [Fact]
        public void GerarCodigoVerificacao_DeveEscaparHtmlEConterCodigoEIdentidadeVisual()
        {
            // Arrange
            var nomeInseguro = "<script>alert('xss')</script>João";
            var codigo = "123456";

            // Act
            var msg = _templateService.GerarCodigoVerificacao("dest@cortex.com", nomeInseguro, codigo, 15);

            // Assert
            Assert.Contains("123456", msg.CorpoHtml);
            Assert.DoesNotContain("<script>", msg.CorpoHtml);
            Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;Jo", msg.CorpoHtml);
            Assert.Contains("#7B4FC9", msg.CorpoHtml); // Roxo Cortex
            Assert.Contains("#12192C", msg.CorpoHtml); // Fundo azul escuro
            Assert.NotNull(msg.CorpoTexto);
            Assert.Contains("123456", msg.CorpoTexto);
        }

        [Fact]
        public void GerarCodigoRedefinicaoSenha_DeveConterAvisoDeSegurancaEEscaparNome()
        {
            // Arrange
            var nomeInseguro = "<b>Maria</b>";
            var codigo = "987654";

            // Act
            var msg = _templateService.GerarCodigoRedefinicaoSenha("dest@cortex.com", nomeInseguro, codigo, 15);

            // Assert
            Assert.Contains("987654", msg.CorpoHtml);
            Assert.DoesNotContain("<b>Maria</b>", msg.CorpoHtml);
            Assert.Contains("&lt;b&gt;Maria&lt;/b&gt;", msg.CorpoHtml);
            Assert.Contains("ignore este e-mail", msg.CorpoHtml);
            Assert.NotNull(msg.CorpoTexto);
            Assert.Contains("987654", msg.CorpoTexto);
        }

        [Fact]
        public void GerarConfirmacaoSenhaAlterada_DeveConterAvisoDeSuporte()
        {
            // Act
            var msg = _templateService.GerarConfirmacaoSenhaAlterada("dest@cortex.com", "Carlos");

            // Assert
            Assert.Contains("sucesso", msg.CorpoHtml);
            Assert.Contains("suporte", msg.CorpoHtml);
            Assert.NotNull(msg.CorpoTexto);
            Assert.Contains("sucesso", msg.CorpoTexto);
        }

        [Fact]
        public void GerarNotificacaoEvento_DeveFormatarLinkComFrontendBaseUrlEEscaparConteudo()
        {
            // Arrange
            var titulo = "<img src=x onerror=alert(1)>Nova Tarefa";
            var mensagem = "Você deve revisar <script>evil()</script>";
            var link = "/tarefas/42";

            // Act
            var msg = _templateService.GerarNotificacaoEvento("dest@cortex.com", "Carlos", "tarefa", titulo, mensagem, link);

            // Assert
            Assert.DoesNotContain("<img src=x", msg.CorpoHtml);
            Assert.DoesNotContain("<script>", msg.CorpoHtml);
            Assert.Contains("https://app.cortex.com/tarefas/42", msg.CorpoHtml);
            Assert.NotNull(msg.CorpoTexto);
            Assert.Contains("https://app.cortex.com/tarefas/42", msg.CorpoTexto);
        }
    }
}

