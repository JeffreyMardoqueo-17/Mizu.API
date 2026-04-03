using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class UsersService : IUsersService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IRoleMutationGuard _roleMutationGuard;
    private readonly IUnitOfWork _unitOfWork;

    public UsersService(IUsuarioRepository usuarioRepository, IRolRepository rolRepository, IRoleMutationGuard roleMutationGuard, IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
        _roleMutationGuard = roleMutationGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPaginatedResponseDto> GetUsersAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        int page,
        int pageSize,
        string? dui,
        string? nombre,
        string? correo,
        bool? estado,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (items, total) = await _usuarioRepository.ListarAvanzadoPorTenantAsync(
            actorTenantId,
            dui,
            nombre,
            correo,
            estado,
            safePage,
            safePageSize,
            cancellationToken: cancellationToken);

        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)safePageSize);

        return new UserPaginatedResponseDto(
            items.Select(ToListItem).ToList(),
            safePage,
            safePageSize,
            total,
            totalPages,
            safePage < totalPages,
            safePage > 1);
    }

    public async Task<UserCreateResponseDto> CreateUserAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var rolSocio = await _rolRepository.ObtenerPorNombreAsync(SystemRoles.Socio, transaction, cancellationToken)
                    ?? throw new InvalidOperationException("No se encontro el rol Socio.");

                var usuario = new Usuario
                {
                    TenantId = actorTenantId,
                    Nombre = request.Nombre.Trim(),
                    Apellido = request.Apellido.Trim(),
                    DUI = request.DUI.Trim(),
                    Correo = request.Correo.Trim(),
                    Telefono = request.Telefono.Trim(),
                    Direccion = request.Direccion.Trim(),
                    PasswordHash = string.Empty,
                    Rol = rolSocio.Nombre,
                    Activo = true,
                    Eliminado = false,
                    MustChangePassword = false,
                    FechaActualizacion = DateTime.UtcNow,
                    TempPasswordGeneratedAt = null,
                    TempPasswordViewedAt = null
                };

                await _usuarioRepository.CrearUsuarioAsync(usuario, transaction, cancellationToken);
                await _usuarioRepository.AsignarRolAsync(usuario.Id, rolSocio.Id, rolSocio.Nombre, transaction, cancellationToken);

                var detail = ToDetail(usuario);
                return new UserCreateResponseDto(detail, string.Empty);
            },
            cancellationToken);
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(Guid actorUsuarioId, Guid actorTenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);
        var usuario = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(userId, actorTenantId, cancellationToken: cancellationToken);
        return usuario is null ? null : ToDetail(usuario);
    }

    public async Task<UserDetailDto?> UpdateUserAsync(Guid actorUsuarioId, Guid actorTenantId, Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var current = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(userId, actorTenantId, cancellationToken: cancellationToken);
        if (current is null)
        {
            return null;
        }

        current.Nombre = request.Nombre.Trim();
        current.Apellido = request.Apellido.Trim();
        current.DUI = request.DUI.Trim();
        current.Correo = request.Correo.Trim();
        current.Telefono = request.Telefono.Trim();
        current.Direccion = request.Direccion.Trim();
        current.Activo = request.Activo;
        current.FechaActualizacion = DateTime.UtcNow;

        var updated = await _usuarioRepository.ActualizarDatosAsync(current, cancellationToken: cancellationToken);
        return updated is null ? null : ToDetail(updated);
    }

    public async Task<bool> SoftDeleteUserAsync(Guid actorUsuarioId, Guid actorTenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);
        if (actorUsuarioId == userId)
        {
            throw new InvalidOperationException("No puedes eliminar logicamente tu propio usuario.");
        }

        return await _usuarioRepository.EliminarLogicoAsync(userId, actorTenantId, cancellationToken: cancellationToken);
    }

    private async Task EnsureAdminAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken)
    {
        var actor = await _usuarioRepository.ObtenerPorIdYTenantAsync(actorUsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (actor is null)
        {
            throw new UnauthorizedAccessException("Usuario autenticado invalido.");
        }

        if (!SystemRoles.EsRolAdministradorDeUsuarios(actor.Rol))
        {
            throw new UnauthorizedAccessException("No tienes permisos para administrar usuarios.");
        }
    }

    private static UserListItemDto ToListItem(Usuario usuario)
    {
        return new UserListItemDto(
            usuario.Id,
            usuario.TenantId,
            usuario.Nombre,
            usuario.Apellido,
            usuario.DUI,
            usuario.Correo,
            usuario.Telefono,
            usuario.Rol,
            usuario.Activo,
            usuario.FechaCreacion,
            usuario.FechaActualizacion);
    }

    private static UserDetailDto ToDetail(Usuario usuario)
    {
        return new UserDetailDto(
            usuario.Id,
            usuario.TenantId,
            usuario.Nombre,
            usuario.Apellido,
            usuario.DUI,
            usuario.Correo,
            usuario.Telefono,
            usuario.Direccion,
            usuario.Rol,
            usuario.Activo,
            usuario.Eliminado,
            usuario.MustChangePassword,
            usuario.FechaCreacion,
            usuario.FechaActualizacion);
    }

    private void EnsureRoleMutationNotBlocked(Guid tenantId)
    {
        if (_roleMutationGuard.IsRoleMutationBlocked(tenantId))
        {
            throw new InvalidOperationException("No se permiten cambios de roles mientras se procesa una activacion de directiva.");
        }
    }
}
