using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IUsuarioAdministracionService
{
    Task<UsuarioGestionResponseDto> CrearUsuarioAsync(CrearUsuarioDto request, Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default);
    Task<UsuarioGestionResponseDto?> ActualizarRolAsync(Guid usuarioId, ActualizarRolUsuarioDto request, Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default);
    Task<UsuarioListadoResponseDto> ListarUsuariosAsync(Guid actorUsuarioId, Guid actorTenantId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
