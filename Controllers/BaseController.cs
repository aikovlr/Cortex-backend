using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    /// <summary>
    /// Controller base com utilitários comuns de identidade e resposta HTTP.
    /// Todos os controllers do Cortex herdam desta classe.
    /// </summary>
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Extrai o ID do usuário autenticado a partir das claims do JWT.
        /// Lança UnauthorizedAccessException se o claim não existir ou for inválido.
        /// </summary>
        protected int ObterUsuarioIdAutenticado()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(claim, out var id))
                return id;

            throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido no token.");
        }
    }
}
