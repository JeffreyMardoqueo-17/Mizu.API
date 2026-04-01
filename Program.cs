using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Muzu.Api.Extensions;

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
        ?? new[]
        {
            "http://localhost:3000",
            "http://localhost:3001",
            "http://192.168.1.206:3000",
            "http://192.168.1.206:3001"
        };

    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddPersistence();
builder.Services.AddApplicationServices();

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
        });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
