using System.Net;
using System.Text.Json;
using Cortex.Infra.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Infra.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var problemDetails = exception switch
            {
                KeyNotFoundException keyNotFound => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "Recurso não encontrado",
                    Detail = keyNotFound.Message,
                    Instance = context.Request.Path
                },
                ForbiddenException forbidden => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Forbidden,
                    Title = "Acesso proibido",
                    Detail = forbidden.Message,
                    Instance = context.Request.Path
                },
                UnauthorizedAccessException unauthorized => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Não autorizado",
                    Detail = unauthorized.Message,
                    Instance = context.Request.Path
                },
                InvalidOperationException invalidOp when invalidOp.InnerException is Npgsql.NpgsqlException || invalidOp.InnerException is System.Net.Sockets.SocketException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.ServiceUnavailable,
                    Title = "Banco de dados indisponível",
                    Detail = "Não foi possível conectar ao servidor PostgreSQL (127.0.0.1:5432). Verifique se o serviço do banco está ativo.",
                    Instance = context.Request.Path
                },
                Npgsql.NpgsqlException => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.ServiceUnavailable,
                    Title = "Banco de dados indisponível",
                    Detail = "Falha de comunicação com o banco de dados PostgreSQL. Verifique se o serviço está em execução.",
                    Instance = context.Request.Path
                },
                InvalidOperationException invalidOp => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Conflict,
                    Title = "Conflito na operação",
                    Detail = invalidOp.Message,
                    Instance = context.Request.Path
                },
                ArgumentException argument => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Requisição inválida",
                    Detail = argument.Message,
                    Instance = context.Request.Path
                },
                _ => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro inesperado no processamento da solicitação.",
                    Instance = context.Request.Path
                }
            };

            if (problemDetails.Status >= 500)
            {
                _logger.LogError(exception, "Erro não tratado durante a requisição {Path}: {Message}", 
                    context.Request.Path, exception.Message);
            }
            else
            {
                _logger.LogWarning("Erro operacional {Status} na requisição {Path}: {Message}", 
                    problemDetails.Status, context.Request.Path, exception.Message);
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

            var json = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        }
    }
}
