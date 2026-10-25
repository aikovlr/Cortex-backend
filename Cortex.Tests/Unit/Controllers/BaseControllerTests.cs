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
    }
}
