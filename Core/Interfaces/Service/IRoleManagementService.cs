using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IRoleManagementService
{
    Task<IReadOnlyList<RoleDto>> ObtenerTodosLosRolesAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default);
    Task<RolePermissionResponseDto?> ObtenerPermisosDelRolAsync(Guid actorUsuarioId, Guid actorTenantId, Guid rolId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermisoDto>> ObtenerTodosLosPermisosAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default);
    Task ActualizarPermisosDelRolAsync(Guid actorUsuarioId, Guid actorTenantId, Guid rolId, List<string> permisoCodigos, CancellationToken cancellationToken = default);
}
