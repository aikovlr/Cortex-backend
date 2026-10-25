using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cortex.Entities;
using Cortex.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cortex.Services.Implementations
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GerarToken(Usuario usuario, string? cargo = null, int? equipeId = null)
        {
            var secret = _configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("Configuração 'Jwt:Secret' não encontrada.");
            var issuer = _configuration["Jwt:Issuer"] ?? "CortexApi";
            var audience = _configuration["Jwt:Audience"] ?? "CortexClient";
            var expiresInMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var exp) ? exp : 1440;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, usuario.Email),
                new(ClaimTypes.Email, usuario.Email),
                new(ClaimTypes.Name, usuario.Nome),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // O token passa a conter apenas dados de identidade: sub, email, name, jti
            // A autorização de cargos é contextual por equipe via IEquipeAuthorizationService
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
