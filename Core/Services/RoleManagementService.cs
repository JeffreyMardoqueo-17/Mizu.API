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

    public async Task<RoleDto> CrearRolAsync(Guid actorUsuarioId, Guid actorTenantId, string nombre, string? descripcion, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var nombreNormalizado = (nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new InvalidOperationException("El nombre del rol es obligatorio.");
        }

        var existente = await _rolRepository.ObtenerPorNombreAsync(nombreNormalizado, cancellationToken: cancellationToken);
        if (existente is not null)
        {
            throw new InvalidOperationException("Ya existe un rol con ese nombre.");
        }

        return await _rolRepository.CrearAsync(nombreNormalizado, string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(), cancellationToken: cancellationToken);
    }

    public async Task<RoleDto?> ActualizarRolAsync(Guid actorUsuarioId, Guid actorTenantId, Guid rolId, string nombre, string? descripcion, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var actual = await _rolRepository.ObtenerPorIdAsync(rolId, cancellationToken: cancellationToken);
        if (actual is null)
        {
            return null;
        }

        if (actual.EsSistema)
        {
            throw new InvalidOperationException("Los roles del sistema no se pueden editar.");
        }

        var nombreNormalizado = (nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new InvalidOperationException("El nombre del rol es obligatorio.");
        }

        var existente = await _rolRepository.ObtenerPorNombreAsync(nombreNormalizado, cancellationToken: cancellationToken);
        if (existente is not null && existente.Id != rolId)
        {
            throw new InvalidOperationException("Ya existe un rol con ese nombre.");
        }

        return await _rolRepository.ActualizarAsync(rolId, nombreNormalizado, string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(), cancellationToken: cancellationToken);
    }

    public async Task EliminarRolAsync(Guid actorUsuarioId, Guid actorTenantId, Guid rolId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var actual = await _rolRepository.ObtenerPorIdAsync(rolId, cancellationToken: cancellationToken);
        if (actual is null)
        {
            throw new InvalidOperationException("El rol no existe.");
        }

        if (actual.EsSistema)
        {
            throw new InvalidOperationException("Los roles del sistema no se pueden eliminar.");
        }

        var asignados = await _rolRepository.ContarAsignacionesActivasAsync(rolId, cancellationToken: cancellationToken);
        if (asignados > 0)
        {
            throw new InvalidOperationException("No se puede eliminar un rol con usuarios activos asignados.");
        }

        var eliminado = await _rolRepository.EliminarAsync(rolId, cancellationToken: cancellationToken);
        if (!eliminado)
        {
            throw new InvalidOperationException("No fue posible eliminar el rol.");
        }
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

        // El rol Socio se mantiene protegido para evitar bloquear acceso básico del tenant.
        if (rol.EsSistema && string.Equals(rol.Nombre, SystemRoles.Socio, StringComparison.OrdinalIgnoreCase))
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
