using System;
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
        private readonly IJwtService _jwtService;

        public AuthService(
            ITenantRepository tenantRepo,
            IUsuarioRepository usuarioRepo,
            ITenantConfigRepository configRepo,
            IJwtService jwtService)
        {
            _tenantRepo = tenantRepo;
            _usuarioRepo = usuarioRepo;
            _configRepo = configRepo;
            _jwtService = jwtService;
        }

        public async Task<(Usuario usuario, Tenant tenant, string token)> RegistrarPrimerTenantYUsuarioAsync(TenantRegistroDto tenantDto, UsuarioRegistroDto usuarioDto)
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

            var token = _jwtService.GenerarToken(usuario.Id, tenant.Id, usuario.Rol);
            return (usuario, tenant, token);
        }

        public async Task<(string token, Guid tenantId)?> LoginAsync(LoginDto loginDto)
        {
            var usuario = await _usuarioRepo.ObtenerPorCorreoAsync(loginDto.Correo);
            if (usuario == null) return null;
            if (!PasswordHasher.VerifyPassword(loginDto.Password, usuario.PasswordHash)) return null;
            
            var token = _jwtService.GenerarToken(usuario.Id, usuario.TenantId, usuario.Rol);
            return (token, usuario.TenantId);
        }
    }
}
