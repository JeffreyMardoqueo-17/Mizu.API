using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class BoardsService : IBoardsService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IRoleMutationGuard _roleMutationGuard;
    private readonly IUnitOfWork _unitOfWork;

    public BoardsService(
        IBoardRepository boardRepository,
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailService emailService,
        IRoleMutationGuard roleMutationGuard,
        IUnitOfWork unitOfWork)
    {
        _boardRepository = boardRepository;
        _usuarioRepository = usuarioRepository;
        _rolRepository = rolRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _emailService = emailService;
        _roleMutationGuard = roleMutationGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BoardListItemDto>> GetBoardsAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var boards = await _boardRepository.ListByTenantAsync(actorTenantId, cancellationToken: cancellationToken);
        return boards.Select(b => new BoardListItemDto(
            b.Id,
            b.Nombre,
            b.Slug,
            DateOnly.FromDateTime(b.FechaInicio),
            DateOnly.FromDateTime(b.FechaFin),
            b.Estado,
            b.FechaCreacion)).ToList();
    }

    public async Task<BoardDetailDto?> GetBoardByIdAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var board = await _boardRepository.GetByIdAsync(boardId, actorTenantId, cancellationToken: cancellationToken);
        if (board is null)
        {
            return null;
        }

        return await BuildBoardDetailAsync(board, cancellationToken);
    }

    public async Task<BoardDetailDto> CreateBoardAsync(Guid actorUsuarioId, Guid actorTenantId, CreateBoardRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        if (request.FechaFin <= request.FechaInicio)
        {
            throw new InvalidOperationException("La fecha fin debe ser mayor a la fecha inicio.");
        }

        await EnsureValidResponsibleAsync(request.AdministradorResponsableId, actorTenantId, cancellationToken);

        var slug = await BuildUniqueSlugAsync(actorTenantId, request.Nombre, null, cancellationToken);

        var board = new Board
        {
            TenantId = actorTenantId,
            Nombre = request.Nombre.Trim(),
            Slug = slug,
            FechaInicio = request.FechaInicio.ToDateTime(TimeOnly.MinValue),
            FechaFin = request.FechaFin.ToDateTime(TimeOnly.MinValue),
            Estado = "Borrador",
            AdministradorResponsableId = request.AdministradorResponsableId,
            FechaActualizacion = DateTime.UtcNow
        };

        await _boardRepository.CreateAsync(board, cancellationToken: cancellationToken);

        await _boardRepository.AddHistoryAsync(new BoardHistoryItem
        {
            BoardId = board.Id,
            Evento = "BOARD_CREATED",
            Descripcion = "Directiva creada en estado borrador.",
            ActorUsuarioId = actorUsuarioId
        }, cancellationToken: cancellationToken);

        return await BuildBoardDetailAsync(board, cancellationToken);
    }

    public async Task<BoardDetailDto?> UpdateBoardAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, UpdateBoardRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        if (request.FechaFin <= request.FechaInicio)
        {
            throw new InvalidOperationException("La fecha fin debe ser mayor a la fecha inicio.");
        }

        var board = await _boardRepository.GetByIdAsync(boardId, actorTenantId, cancellationToken: cancellationToken);
        if (board is null)
        {
            return null;
        }

        await EnsureValidResponsibleAsync(request.AdministradorResponsableId, actorTenantId, cancellationToken);

        board.Nombre = request.Nombre.Trim();
        board.Slug = await BuildUniqueSlugAsync(actorTenantId, request.Nombre, board.Id, cancellationToken);
        board.FechaInicio = request.FechaInicio.ToDateTime(TimeOnly.MinValue);
        board.FechaFin = request.FechaFin.ToDateTime(TimeOnly.MinValue);
        board.AdministradorResponsableId = request.AdministradorResponsableId;
        board.FechaActualizacion = DateTime.UtcNow;

        await _boardRepository.UpdateAsync(board, cancellationToken: cancellationToken);
        await _boardRepository.AddHistoryAsync(new BoardHistoryItem
        {
            BoardId = board.Id,
            Evento = "BOARD_UPDATED",
            Descripcion = "Se actualizaron los datos de la directiva.",
            ActorUsuarioId = actorUsuarioId
        }, cancellationToken: cancellationToken);

        return await BuildBoardDetailAsync(board, cancellationToken);
    }

    public async Task<BoardDetailDto?> AddMemberAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, AddBoardMemberRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var board = await _boardRepository.GetByIdAsync(boardId, actorTenantId, cancellationToken: cancellationToken);
        if (board is null)
        {
            return null;
        }

        var user = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(request.UsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (user is null || user.Eliminado || !user.Activo)
        {
            throw new InvalidOperationException("El usuario no existe o no esta activo.");
        }

        if (!string.Equals(user.Rol, SystemRoles.Socio, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Solo usuarios con rol base Socio pueden agregarse a directiva.");
        }

        var rol = await _rolRepository.ObtenerPorNombreAsync(request.Rol, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("El rol indicado no existe en catalogo de roles.");

        var member = new BoardMember
        {
            BoardId = boardId,
            UsuarioId = user.Id,
            RolId = rol.Id,
            Cargo = rol.Nombre
        };

        var accessPassword = GeneratePeriodPassword();
        var accessPasswordHash = PasswordHasher.HashPassword(accessPassword);
        var memberNotifications = new List<CredentialNotification>();

        await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                await _boardRepository.AddMemberAsync(member, transaction, cancellationToken);

                await _usuarioRepository.AsignarRolAsync(user.Id, rol.Id, rol.Nombre, transaction, cancellationToken);

                await _usuarioRepository.SetActiveStateAsync(user.Id, actorTenantId, true, transaction, cancellationToken);
                await _usuarioRepository.EstablecerPasswordDePeriodoAsync(user.Id, actorTenantId, accessPasswordHash, transaction, cancellationToken);

                await _boardRepository.AddHistoryAsync(new BoardHistoryItem
                {
                    BoardId = boardId,
                    Evento = "MEMBER_ADDED",
                    Descripcion = $"Se agrego usuario {user.Nombre} {user.Apellido} con rol {rol.Nombre} y se generaron credenciales de acceso.",
                    ActorUsuarioId = actorUsuarioId
                }, transaction, cancellationToken);

                memberNotifications.Add(new CredentialNotification(
                    user.Correo,
                    $"{user.Nombre} {user.Apellido}".Trim(),
                    rol.Nombre,
                    accessPassword));

                return true;
            },
            cancellationToken);

        foreach (var notification in memberNotifications)
        {
            await _emailService.SendBoardMemberCredentialsAsync(
                notification.Email,
                notification.NombreUsuario,
                notification.Rol,
                board.Nombre,
                DateOnly.FromDateTime(board.FechaInicio),
                DateOnly.FromDateTime(board.FechaFin),
                notification.Password,
                cancellationToken);
        }

        return await BuildBoardDetailAsync(board, cancellationToken);
    }

    public async Task<BoardActivationResponseDto?> ActivateBoardAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);
        await using var roleMutationWindow = await _roleMutationGuard.EnterRoleMutationWindowAsync(actorTenantId, cancellationToken);

        var response = await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var board = await _boardRepository.GetByIdAsync(boardId, actorTenantId, transaction, cancellationToken);
                if (board is null)
                {
                    return null;
                }

                var active = await _boardRepository.GetActiveBoardAsync(actorTenantId, transaction, cancellationToken);
                if (active is not null && active.Id != board.Id)
                {
                    // Deactivate old board and return all previous members to Socio.
                    await _boardRepository.SetBoardStateAsync(active.Id, "Inactiva", DateTime.UtcNow, transaction, cancellationToken);
                    var oldMembers = await _boardRepository.GetMembersAsync(active.Id, transaction, cancellationToken);
                    var rolSocio = await _rolRepository.ObtenerPorNombreAsync(SystemRoles.Socio, transaction, cancellationToken)
                        ?? throw new InvalidOperationException("No se encontro rol Socio.");

                    foreach (var oldMember in oldMembers)
                    {
                        await _usuarioRepository.AsignarRolAsync(oldMember.UsuarioId, rolSocio.Id, rolSocio.Nombre, transaction, cancellationToken);
                        await _usuarioRepository.SetActiveStateAsync(oldMember.UsuarioId, actorTenantId, false, transaction, cancellationToken);
                        await _refreshTokenRepository.RevocarTodosDelUsuarioAsync(oldMember.UsuarioId, transaction, cancellationToken);
                    }
                }

                var members = await _boardRepository.GetMembersAsync(board.Id, transaction, cancellationToken);
                if (members.Count == 0)
                {
                    throw new InvalidOperationException("No se puede activar una directiva sin miembros.");
                }

                foreach (var member in members)
                {
                    await _usuarioRepository.AsignarRolAsync(member.UsuarioId, member.RolId, member.Cargo, transaction, cancellationToken);
                    await _usuarioRepository.SetActiveStateAsync(member.UsuarioId, actorTenantId, true, transaction, cancellationToken);
                    await _refreshTokenRepository.RevocarTodosDelUsuarioAsync(member.UsuarioId, transaction, cancellationToken);
                }

                await _boardRepository.SetBoardStateAsync(board.Id, "Activa", DateTime.UtcNow, transaction, cancellationToken);
                await _boardRepository.AddHistoryAsync(new BoardHistoryItem
                {
                    BoardId = board.Id,
                    Evento = "BOARD_ACTIVATED",
                    Descripcion = "Directiva activada de forma atomica y roles aplicados con bloqueo de mutaciones concurrentes.",
                    ActorUsuarioId = actorUsuarioId
                }, transaction, cancellationToken);

                return new BoardActivationResponseDto(board.Id, "Activa", DateTime.UtcNow);
            },
            cancellationToken);

        if (response is null)
        {
            return null;
        }

        return response;
    }

    public async Task<IReadOnlyList<BoardHistoryItemDto>> GetBoardHistoryAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var board = await _boardRepository.GetByIdAsync(boardId, actorTenantId, cancellationToken: cancellationToken);
        if (board is null)
        {
            return [];
        }

        var history = await _boardRepository.GetHistoryAsync(boardId, cancellationToken: cancellationToken);
        return history.Select(h => new BoardHistoryItemDto(h.Id, h.BoardId, h.Evento, h.Descripcion, h.ActorUsuarioId, h.FechaCreacion)).ToList();
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
            throw new UnauthorizedAccessException("No tienes permisos para gestionar directivas.");
        }
    }

    private async Task<BoardDetailDto> BuildBoardDetailAsync(Board board, CancellationToken cancellationToken)
    {
        var members = await _boardRepository.GetMembersAsync(board.Id, cancellationToken: cancellationToken);
        var memberDtos = new List<BoardMemberDto>();

        foreach (var member in members)
        {
            var user = await _usuarioRepository.ObtenerPorIdAsync(member.UsuarioId, cancellationToken: cancellationToken);
            if (user is null)
            {
                continue;
            }

            memberDtos.Add(new BoardMemberDto(
                member.UsuarioId,
                $"{user.Nombre} {user.Apellido}",
                user.DUI,
                member.Cargo,
                member.FechaCreacion));
        }

        return new BoardDetailDto(
            board.Id,
            board.TenantId,
            board.Nombre,
            board.Slug,
            DateOnly.FromDateTime(board.FechaInicio),
            DateOnly.FromDateTime(board.FechaFin),
            board.Estado,
            board.AdministradorResponsableId,
            board.FechaCreacion,
            board.FechaActualizacion,
            board.FechaTransicion,
            memberDtos);
    }

    private static string BuildSlug(string source)
    {
        var normalized = source.Trim().ToLowerInvariant();
        var chars = normalized
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var raw = new string(chars);
        while (raw.Contains("--", StringComparison.Ordinal))
        {
            raw = raw.Replace("--", "-", StringComparison.Ordinal);
        }

        return raw.Trim('-');
    }

    private async Task<string> BuildUniqueSlugAsync(Guid tenantId, string source, Guid? excludingBoardId, CancellationToken cancellationToken)
    {
        var baseSlug = BuildSlug(source);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "directiva";
        }

        var candidate = baseSlug;
        var suffix = 1;

        while (await _boardRepository.ExistsSlugAsync(tenantId, candidate, excludingBoardId, cancellationToken: cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private async Task EnsureValidResponsibleAsync(Guid responsableId, Guid tenantId, CancellationToken cancellationToken)
    {
        var responsable = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(responsableId, tenantId, cancellationToken: cancellationToken);
        if (responsable is null || responsable.Eliminado)
        {
            throw new InvalidOperationException("El administrador responsable no es valido para este tenant.");
        }
    }

    private static string GeneratePeriodPassword()
    {
        const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#";
        var random = new Random();
        var length = random.Next(8, 11);
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = allowed[random.Next(allowed.Length)];
        }

        return new string(chars);
    }

    private sealed record CredentialNotification(
        string Email,
        string NombreUsuario,
        string Rol,
        string Password);
}
