using System.Text;
using System.Security.Claims;
using CloudinaryDotNet;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Rules;
using Muzu.Api.Extensions;
using Muzu.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Frontend:Origins")
        .Get<string[]>()
        ?? Array.Empty<string>();

    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)
                || origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase)
                || allowedOrigins.Any(o => string.Equals(o.TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddPersistence();
builder.Services.AddApplicationServices();

// Configurar Cloudinary desde environment variable o appsettings
var cloudinaryUrl = Environment.GetEnvironmentVariable("URL__Cloudinary")
    ?? builder.Configuration["Cloudinary:Url"]
    ?? throw new InvalidOperationException("No se encontró configuración de Cloudinary. Configura URL__Cloudinary o Cloudinary:Url.");

var cloudinary = new Cloudinary(cloudinaryUrl);
builder.Services.AddSingleton(cloudinary);

var jwtSecret =
    Environment.GetEnvironmentVariable("MUZU_JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("No se encontro MUZU_JWT_SECRET.");

var jwtIssuer =
    Environment.GetEnvironmentVariable("MUZU_JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "MuzuApi";

var jwtAudience =
    Environment.GetEnvironmentVariable("MUZU_JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "MuzuApi";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrWhiteSpace(context.Token)
                        && context.Request.Cookies.TryGetValue("muzu_token", out var cookieToken)
                        && !string.IsNullOrWhiteSpace(cookieToken))
                        context.Token = cookieToken;
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (principal is null)
                    {
                        context.Fail("Token invalido.");
                        return;
                    }

                    var role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                    if (SystemRoles.EsAdministrador(role) || !SystemRoles.EsRolDeDirectiva(role))
                        return;

                    var userIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    var tenantIdRaw = principal.FindFirstValue("tenant_id");

                    if (!Guid.TryParse(userIdRaw, out var userId) || !Guid.TryParse(tenantIdRaw, out var tenantId))
                    {
                        context.Fail("Claims invalidos.");
                        return;
                    }

                    var boardRepository = context.HttpContext.RequestServices.GetRequiredService<IBoardRepository>();
                    var accessAllowed = await boardRepository.IsUserInActiveBoardAsync(tenantId, userId, cancellationToken: context.HttpContext.RequestAborted);

                    if (!accessAllowed)
                        context.Fail("DIRECTIVA_NOT_ACTIVE");
                    
                }
            };
        });

var app = builder.Build();

var runningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

await app.EnsureDatabaseSchemaAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseMiddleware<ExceptionLoggingMiddleware>();
app.UseMiddleware<RequestDiagnosticsMiddleware>();

if (!runningInContainer)
    app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
