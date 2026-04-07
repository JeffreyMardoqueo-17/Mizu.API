using System.Diagnostics;
using System.Security.Claims;

namespace Muzu.Api.Middlewares;

public sealed class RequestDiagnosticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDiagnosticsMiddleware> _logger;

    public RequestDiagnosticsMiddleware(RequestDelegate next, ILogger<RequestDiagnosticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var stopwatch = Stopwatch.StartNew();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var tenantId = context.User.FindFirstValue("tenant_id") ?? "n/a";

        _logger.LogInformation(
            "[HTTP-IN] {Method} {Path} query={QueryString} contentType={ContentType} contentLength={ContentLength} userId={UserId} tenantId={TenantId} traceId={TraceId}",
            request.Method,
            request.Path,
            request.QueryString.HasValue ? request.QueryString.Value : "",
            request.ContentType ?? "",
            request.ContentLength,
            userId,
            tenantId,
            context.TraceIdentifier);

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation(
            "[HTTP-OUT] {Method} {Path} status={StatusCode} elapsedMs={ElapsedMs} traceId={TraceId}",
            request.Method,
            request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}
