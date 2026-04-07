using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Muzu.Api.Middlewares;

public sealed class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
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
            _logger.LogError(
                ex,
                "[HTTP-EXCEPTION] {Method} {Path} traceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new ProblemDetails
            {
                Title = "Error interno",
                Detail = "Ocurrió un error inesperado en el servidor.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            };

            payload.Extensions["traceId"] = context.TraceIdentifier;

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
