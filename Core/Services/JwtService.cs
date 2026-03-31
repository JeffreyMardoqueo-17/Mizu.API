using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Muzu.Api.Core.Services
{
    public interface IJwtService
    {
        string GenerarToken(Guid usuarioId, Guid tenantId, string rol);
    }

    public class JwtService : IJwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtService()
        {
            _secretKey = Environment.GetEnvironmentVariable("MUZU_JWT_SECRET") ?? throw new Exception("No se encontró MUZU_JWT_SECRET");
            _issuer = Environment.GetEnvironmentVariable("MUZU_JWT_ISSUER") ?? "MuzuApi";
            _audience = Environment.GetEnvironmentVariable("MUZU_JWT_AUDIENCE") ?? "MuzuApi";
        }

        public string GenerarToken(Guid usuarioId, Guid tenantId, string rol)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(ClaimTypes.Role, rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
