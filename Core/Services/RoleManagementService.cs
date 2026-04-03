using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class RoleManagementService : IRoleManagementService
{
    private readonly IRolRepository _rolRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public RoleManagementService(
        IRolRepository rolRepository,
        IUsuarioRepository usuarioRepository)
    {
        _rolRepository = rolRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IReadOnlyList<RoleDto>> ObtenerTodosLosRolesAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);
        return await _rolRepository.ObtenerTodosAsync(cancellationToken: cancellationToken);
    }

    public async Task<RolePermissionResponseDto?> ObtenerPermisosDelRolAsync(Guid actorUsuarioId, Guid actorTenantId, Guid rolId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var permisosAsignados = await _rolRepository.ObtenerPermisosPorRolIdAsync(rolId, cancellationToken: cancellationToken);
        var permisosDisponibles = await _rolRepository.ObtenerTodosPermisosAsync(cancellationToken: cancellationToken);

        var roles = await _rolRepository.ObtenerTodosAsync(cancellationToken: cancellationToken);
        var rol = roles.FirstOrDefault(r => r.Id == rolId);

        if (rol is null)
        {
            return null;
        }

        return new RolePermissionResponseDto(
            rolId,
            rol.Nombre,
            permisosDisponibles.ToList(),
            permisosAsignados.ToList());
    }

    public async Task<IReadOnlyList<PermisoDto>> ObtenerTodosLosPermisosAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);
        return await _rolRepository.ObtenerTodosPermisosAsync(cancellationToken: cancellationToken);
    }

    public async Task ActualizarPermisosDelRolAsync(Guid actorUsuarioId, Guid actorTenantId, Guid rolId, List<string> permisoCodigos, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var roles = await _rolRepository.ObtenerTodosAsync(cancellationToken: cancellationToken);
        var rol = roles.FirstOrDefault(r => r.Id == rolId);

        if (rol is null)
        {
            throw new InvalidOperationException("El rol no existe.");
        }

        // No permitir cambios a roles del sistema sin autorización especial
        if (rol.EsSistema && (string.Equals(rol.Nombre, SystemRoles.Administrador, StringComparison.OrdinalIgnoreCase) || string.Equals(rol.Nombre, SystemRoles.Socio, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"No se pueden modificar los permisos del rol del sistema '{rol.Nombre}'.");
        }

        await _rolRepository.ActualizarPermisosDelRolAsync(rolId, permisoCodigos, cancellationToken: cancellationToken);
    }

    private async Task EnsureAdminAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken)
    {
        var actor = await _usuarioRepository.ObtenerPorIdYTenantAsync(actorUsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (actor is null)
        {
            throw new UnauthorizedAccessException("Usuario autenticado inválido.");
        }

        if (!SystemRoles.EsRolAdministradorDeUsuarios(actor.Rol))
        {
            throw new UnauthorizedAccessException("No tienes permisos para administrar roles.");
        }
    }
}
