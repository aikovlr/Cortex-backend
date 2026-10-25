using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cortex.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cortex.Tests.Unit.Controllers
{
    public class TestableController : BaseController
    {
        public int ObterIdPublico() => ObterUsuarioIdAutenticado();
        public IActionResult NaoEncontradoPublico(string detalhe, string titulo = "Recurso não encontrado") => NaoEncontrado(detalhe, titulo);
    }

    public class BaseControllerTests
    {
        [Fact]
        public void ObterUsuarioIdAutenticado_ComNameIdentifierValido_RetornaIdCorreto()
        {
            // Arrange
            var controller = new TestableController();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "42")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var id = controller.ObterIdPublico();

            // Assert
            Assert.Equal(42, id);
        }

        [Fact]
        public void ObterUsuarioIdAutenticado_ComSubValido_RetornaIdCorreto()
        {
            // Arrange
            var controller = new TestableController();
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, "99")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var id = controller.ObterIdPublico();

            // Assert
            Assert.Equal(99, id);
        }

        [Fact]
        public void ObterUsuarioIdAutenticado_SemClaimValida_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new TestableController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => controller.ObterIdPublico());
        }

        [Fact]
        public void NaoEncontrado_RetornaNotFoundObjectResultComProblemDetails()
        {
            // Arrange
            var controller = new TestableController();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = "/api/teste/123";
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = controller.NaoEncontradoPublico("Item não encontrado.", "Teste Não Encontrado");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("Teste Não Encontrado", problemDetails.Title);
            Assert.Equal("Item não encontrado.", problemDetails.Detail);
            Assert.Equal("/api/teste/123", problemDetails.Instance);
        }
    }
}
