using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly ITenantConfigRepository _configRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IJwtService _jwtService;

        public AuthService(
            ITenantRepository tenantRepo,
            IUsuarioRepository usuarioRepo,
            ITenantConfigRepository configRepo,
            IRefreshTokenRepository refreshTokenRepo,
            IJwtService jwtService)
        {
            _tenantRepo = tenantRepo;
            _usuarioRepo = usuarioRepo;
            _configRepo = configRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtService = jwtService;
        }

        public async Task<(Usuario usuario, Tenant tenant, LoginResponseDto loginResponse)> RegistrarPrimerTenantYUsuarioAsync(TenantRegistroDto tenantDto, UsuarioRegistroDto usuarioDto)
        {
            var tenant = new Tenant
            {
                Nombre = tenantDto.Nombre,
                Direccion = tenantDto.Direccion
            };
            await _tenantRepo.CrearTenantAsync(tenant);

            var config = new TenantConfig { TenantId = tenant.Id };
            await _configRepo.CrearConfigAsync(config);

            var usuario = new Usuario
            {
                TenantId = tenant.Id,
                Nombre = usuarioDto.Nombre,
                Apellido = usuarioDto.Apellido,
                DUI = usuarioDto.DUI,
                Correo = usuarioDto.Correo,
                Telefono = usuarioDto.Telefono,
                Direccion = usuarioDto.Direccion,
                PasswordHash = PasswordHasher.HashPassword(usuarioDto.Password),
                Rol = "Administrador"
            };
            await _usuarioRepo.CrearUsuarioAsync(usuario);

            var loginResponse = await GenerarTokensAsync(usuario);
            return (usuario, tenant, loginResponse);
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var usuario = await _usuarioRepo.ObtenerPorCorreoAsync(loginDto.Correo);
            if (usuario == null) return null;
            if (!PasswordHasher.VerifyPassword(loginDto.Password, usuario.PasswordHash)) return null;
            
            return await GenerarTokensAsync(usuario);
        }

        public async Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var tokenExistente = await _refreshTokenRepo.ObtenerPorTokenAsync(refreshToken);
            if (tokenExistente == null) return null;
            if (tokenExistente.Revocado) return null;
            if (tokenExistente.Expira < DateTime.UtcNow) return null;

            var usuario = await _usuarioRepo.ObtenerPorIdAsync(tokenExistente.UsuarioId);
            if (usuario == null) return null;

            await _refreshTokenRepo.RevocarAsync(refreshToken);

            return await GenerarTokensAsync(usuario);
        }

        private async Task<LoginResponseDto> GenerarTokensAsync(Usuario usuario)
        {
            var accessToken = _jwtService.GenerarToken(usuario.Id, usuario.TenantId, usuario.Rol);
            
            var refreshToken = new RefreshToken
            {
                UsuarioId = usuario.Id,
                Token = GenerarRefreshToken(),
                Expira = DateTime.UtcNow.AddHours(24)
            };
            await _refreshTokenRepo.CrearAsync(refreshToken);

            return new LoginResponseDto(
                accessToken,
                refreshToken.Token,
                usuario.TenantId,
                usuario.Id,
                usuario.Rol
            );
        }

        private static string GenerarRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
