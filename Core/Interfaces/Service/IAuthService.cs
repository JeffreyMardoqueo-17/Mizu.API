using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IAuthService
{
    Task<RegisterTenantCommandResultDto> RegistrarPrimerTenantYUsuarioAsync(TenantUsuarioRegistroDto request, CancellationToken cancellationToken = default);
    Task<AuthenticatedCommandResultDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
    Task<AuthenticatedCommandResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
