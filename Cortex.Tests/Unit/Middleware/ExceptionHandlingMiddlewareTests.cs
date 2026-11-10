using System.Net;
using System.Text.Json;
using Cortex.Infra.Exceptions;
using Cortex.Infra.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cortex.Tests.Unit.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();

        [Fact]
        public async Task InvokeAsync_QuandoNaoHaExcecao_ChamaProximoMiddleware()
        {
            var nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);
            var context = new DefaultHttpContext();

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
        }

        [Theory]
        [InlineData(typeof(KeyNotFoundException), 404, "https://tools.ietf.org/html/rfc9110#section-15.5.5", "Recurso não encontrado")]
        [InlineData(typeof(ForbiddenException), 403, "https://tools.ietf.org/html/rfc9110#section-15.5.4", "Acesso proibido")]
        [InlineData(typeof(UnauthorizedAccessException), 401, "https://tools.ietf.org/html/rfc9110#section-15.5.2", "Não autorizado")]
        [InlineData(typeof(ArgumentException), 400, "https://tools.ietf.org/html/rfc9110#section-15.5.1", "Requisição inválida")]
        [InlineData(typeof(InvalidOperationException), 409, "https://tools.ietf.org/html/rfc9110#section-15.5.10", "Conflito na operação")]
        [InlineData(typeof(Exception), 500, "https://tools.ietf.org/html/rfc9110#section-15.6.1", "Erro interno do servidor")]
        public async Task InvokeAsync_QuandoLancaExcecao_RetornaProblemDetailsRFC9457(
            Type exceptionType, 
            int expectedStatus, 
            string expectedType, 
            string expectedTitle)
        {
            // Arrange
            Exception exception = exceptionType switch
            {
                var t when t == typeof(KeyNotFoundException) => new KeyNotFoundException("Item 123 não encontrado."),
                var t when t == typeof(ForbiddenException) => new ForbiddenException("Acesso negado ao recurso."),
                var t when t == typeof(UnauthorizedAccessException) => new UnauthorizedAccessException("Credencial inválida."),
                var t when t == typeof(ArgumentException) => new ArgumentException("Parâmetro inválido."),
                var t when t == typeof(InvalidOperationException) => new InvalidOperationException("Operação conflituosa."),
                _ => new Exception("Falha inesperada.")
            };

            RequestDelegate next = (ctx) => throw exception;
            var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

            var context = new DefaultHttpContext();
            context.Request.Path = "/api/teste/recurso";
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("application/problem+json", context.Response.ContentType);
            Assert.Equal(expectedStatus, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();

            var problem = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(problem);
            Assert.Equal(expectedStatus, problem.Status);
            Assert.Equal(expectedType, problem.Type);
            Assert.Equal(expectedTitle, problem.Title);
            Assert.Equal("/api/teste/recurso", problem.Instance);
            Assert.NotNull(problem.Detail);
        }
    }
}

