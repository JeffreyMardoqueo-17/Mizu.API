using System;
using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Muzu.Api.Core.Interfaces.ITenantRepository, Muzu.Api.Core.Repositories.TenantRepository>();
builder.Services.AddSingleton<Muzu.Api.Core.Interfaces.IUsuarioRepository, Muzu.Api.Core.Repositories.UsuarioRepository>();
builder.Services.AddSingleton<Muzu.Api.Core.Interfaces.ITenantConfigRepository, Muzu.Api.Core.Repositories.TenantConfigRepository>();
builder.Services.AddSingleton<Muzu.Api.Core.Interfaces.IRefreshTokenRepository, Muzu.Api.Core.Repositories.RefreshTokenRepository>();
builder.Services.AddSingleton<Muzu.Api.Core.Services.IJwtService, Muzu.Api.Core.Services.JwtService>();
builder.Services.AddSingleton<Muzu.Api.Core.Services.IAuthService, Muzu.Api.Core.Services.AuthService>();

var jwtSecret = Environment.GetEnvironmentVariable("MUZU_JWT_SECRET") ?? throw new Exception("No se encontró MUZU_JWT_SECRET");
var jwtIssuer = Environment.GetEnvironmentVariable("MUZU_JWT_ISSUER") ?? "MuzuApi";
var jwtAudience = Environment.GetEnvironmentVariable("MUZU_JWT_AUDIENCE") ?? "MuzuApi";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
