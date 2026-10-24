using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cortex.DTOs;
using Cortex.DTOs.Request;
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cortex.Tests.Integration
{
    public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public IntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_ComCredenciaisInvalidas_DeveRetornar401ProblemDetails()
        {
            // Arrange
            var loginDto = new DadosLoginDTO
            {
                Email = "inexistente@cortex.com",
                Senha = "SenhaIncorreta123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(401, problem.Status);
            Assert.Equal("Não autorizado", problem.Title);
        }

        [Fact]
        public async Task VerificarEmail_ComUsuarioCadastrado_DeveRetornarSucesso()
        {
            // Arrange: Seed user into test database
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CortexDbContext>();
                var user = new Usuario
                {
                    Nome = "Usuario Teste Integracao",
                    Email = "seed@cortex.com",
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("SenhaForte123!"),
                    EmailVerificado = false,
                    DataCriacao = DateTime.UtcNow
                };
                db.Usuarios.Add(user);
                await db.SaveChangesAsync();
            }

            var dto = new VerificarEmailDTO
            {
                Email = "seed@cortex.com",
                Codigo = "123456"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/verificar-email", dto);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("sucesso", content);
        }
    }
}
