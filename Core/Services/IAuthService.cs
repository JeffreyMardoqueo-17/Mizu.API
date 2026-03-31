using System;
using System.Threading.Tasks;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Services
{
    public interface IAuthService
    {
        Task<(Usuario usuario, Tenant tenant, string token)> RegistrarPrimerTenantYUsuarioAsync(TenantRegistroDto tenantDto, UsuarioRegistroDto usuarioDto);
        Task<(string token, Guid tenantId)?> LoginAsync(LoginDto loginDto);
    }
}
