using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class UsuarioAdministracionService : IUsuarioAdministracionService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IRoleMutationGuard _roleMutationGuard;
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioAdministracionService(
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        IRoleMutationGuard roleMutationGuard,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
        _roleMutationGuard = roleMutationGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<UsuarioGestionResponseDto> CrearUsuarioAsync(CrearUsuarioDto request, Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default)
    {
        await EnsureActorCanManageUsersAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var existente = await _usuarioRepository.ObtenerPorCorreoAsync(request.Correo, cancellationToken: cancellationToken);
        if (existente is not null)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        var rol = await _rolRepository.ObtenerPorNombreAsync(request.Rol, cancellationToken: cancellationToken);
        if (rol is null)
        {
            throw new InvalidOperationException("El rol indicado no existe.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var usuario = new Usuario
                {
                    TenantId = actorTenantId,
                    Nombre = request.Nombre.Trim(),
                    Apellido = request.Apellido.Trim(),
                    DUI = request.DUI.Trim(),
                    Correo = request.Correo.Trim(),
                    Telefono = request.Telefono.Trim(),
                    Direccion = request.Direccion.Trim(),
                    PasswordHash = PasswordHasher.HashPassword(request.Password),
                    Rol = rol.Nombre
                };

                await _usuarioRepository.CrearUsuarioAsync(usuario, transaction, cancellationToken);
                await _usuarioRepository.AsignarRolAsync(usuario.Id, rol.Id, rol.Nombre, transaction, cancellationToken);

                var permisos = await _rolRepository.ObtenerPermisosPorRolIdAsync(rol.Id, transaction, cancellationToken);
                return ToResponse(usuario, permisos);
            },
            cancellationToken);
    }

    public async Task<UsuarioGestionResponseDto?> ActualizarRolAsync(Guid usuarioId, ActualizarRolUsuarioDto request, Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default)
    {
        await EnsureActorCanManageUsersAsync(actorUsuarioId, actorTenantId, cancellationToken);
        EnsureRoleMutationNotBlocked(actorTenantId);

        var rolNuevo = await _rolRepository.ObtenerPorNombreAsync(request.Rol, cancellationToken: cancellationToken);
        if (rolNuevo is null)
        {
            throw new InvalidOperationException("El rol indicado no existe.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var usuarioObjetivo = await _usuarioRepository.ObtenerPorIdYTenantAsync(usuarioId, actorTenantId, transaction, cancellationToken);
                if (usuarioObjetivo is null)
                {
                    return null;
                }

                await ValidateSelfDemotionRuleAsync(
                    usuarioObjetivo,
                    rolNuevo.Nombre,
                    actorUsuarioId,
                    actorTenantId,
                    transaction,
                    cancellationToken);

                await _usuarioRepository.AsignarRolAsync(usuarioObjetivo.Id, rolNuevo.Id, rolNuevo.Nombre, transaction, cancellationToken);
                usuarioObjetivo.Rol = rolNuevo.Nombre;

                var permisos = await _rolRepository.ObtenerPermisosPorRolIdAsync(rolNuevo.Id, transaction, cancellationToken);
                return ToResponse(usuarioObjetivo, permisos);
            },
            cancellationToken);
    }

    public async Task<UsuarioListadoResponseDto> ListarUsuariosAsync(Guid actorUsuarioId, Guid actorTenantId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await EnsureActorCanManageUsersAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var (items, total) = await _usuarioRepository.ListarPorTenantAsync(
            actorTenantId,
            search,
            safePage,
            safePageSize,
            cancellationToken: cancellationToken);

        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)safePageSize);
        var responseItems = items
            .Select(usuario => new UsuarioListaItemDto(
                usuario.Id,
                usuario.TenantId,
                usuario.Nombre,
                usuario.Apellido,
                usuario.DUI,
                usuario.Correo,
                usuario.Telefono,
                usuario.Rol,
                usuario.FechaCreacion))
            .ToList();

        return new UsuarioListadoResponseDto(
            responseItems,
            safePage,
            safePageSize,
            total,
            totalPages,
            safePage < totalPages,
            safePage > 1);
    }

    private async Task EnsureActorCanManageUsersAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken)
    {
        var actor = await _usuarioRepository.ObtenerPorIdYTenantAsync(actorUsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (actor is null)
        {
            throw new UnauthorizedAccessException("Usuario autenticado invalido para este tenant.");
        }

        if (!SystemRoles.EsRolAdministradorDeUsuarios(actor.Rol))
        {
            throw new UnauthorizedAccessException("No tienes permisos para gestionar usuarios.");
        }
    }

    private async Task ValidateSelfDemotionRuleAsync(
        Usuario usuarioObjetivo,
        string nuevoRol,
        Guid actorUsuarioId,
        Guid actorTenantId,
        System.Data.IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var isSelfUpdate = usuarioObjetivo.Id == actorUsuarioId;
        if (!isSelfUpdate)
        {
            return;
        }

        if (!SystemRoles.EsRolProtegidoParaAutodemocion(usuarioObjetivo.Rol))
        {
            return;
        }

        if (SystemRoles.EsRolProtegidoParaAutodemocion(nuevoRol))
        {
            return;
        }

        var existeOtroConRol = await _usuarioRepository.ExisteOtroUsuarioConRolAsync(
            actorTenantId,
            usuarioObjetivo.Rol,
            actorUsuarioId,
            transaction,
            cancellationToken);

        if (!existeOtroConRol)
        {
            throw new InvalidOperationException($"No puedes quitarte el rol '{usuarioObjetivo.Rol}' porque no existe otro usuario con ese rol.");
        }
    }

    private static UsuarioGestionResponseDto ToResponse(Usuario usuario, IReadOnlyList<string> permisos)
    {
        return new UsuarioGestionResponseDto(
            usuario.Id,
            usuario.TenantId,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Correo,
            usuario.Telefono,
            usuario.Direccion,
            usuario.Rol,
            permisos,
            usuario.FechaCreacion);
    }

    private void EnsureRoleMutationNotBlocked(Guid tenantId)
    {
        if (_roleMutationGuard.IsRoleMutationBlocked(tenantId))
        {
            throw new InvalidOperationException("No se permiten cambios de roles mientras se procesa una activacion de directiva.");
        }
    }
}
