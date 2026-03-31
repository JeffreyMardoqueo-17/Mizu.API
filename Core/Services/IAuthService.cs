using System;
using System.Threading.Tasks;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Services
{
    public interface IAuthService
    {
        Task<(Usuario usuario, Tenant tenant, LoginResponseDto loginResponse)> RegistrarPrimerTenantYUsuarioAsync(TenantRegistroDto tenantDto, UsuarioRegistroDto usuarioDto);
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto?> RefreshTokenAsync(string refreshToken);
    }
}
