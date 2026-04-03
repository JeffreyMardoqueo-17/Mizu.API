using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class AuthSecurityService : IAuthSecurityService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthSecurityService(
        IUsuarioRepository usuarioRepository,
        IBoardRepository boardRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _usuarioRepository = usuarioRepository;
        _boardRepository = boardRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> ChangeTemporaryPasswordAsync(Guid actorUsuarioId, Guid actorTenantId, string newPassword, CancellationToken cancellationToken = default)
    {
        var actor = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(actorUsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (actor is null || actor.Eliminado)
        {
            throw new UnauthorizedAccessException("Sesion invalida.");
        }

        var newHash = PasswordHasher.HashPassword(newPassword);
        return await _usuarioRepository.CambiarPasswordDefinitivaAsync(actorUsuarioId, actorTenantId, newHash, cancellationToken: cancellationToken);
    }

    public async Task<RegenerateTemporaryPasswordResponseDto> RegenerateTemporaryPasswordAsync(Guid actorUsuarioId, Guid actorTenantId, Guid targetUsuarioId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var target = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(targetUsuarioId, actorTenantId, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("El usuario objetivo no existe en este tenant.");

        var tempPassword = GenerateTemporaryPassword();
        var hash = PasswordHasher.HashPassword(tempPassword);
        await _usuarioRepository.EstablecerPasswordTemporalAsync(target.Id, actorTenantId, hash, true, cancellationToken: cancellationToken);

        return new RegenerateTemporaryPasswordResponseDto(target.Id, tempPassword);
    }

    public async Task<int> InvalidateBoardSessionsAsync(Guid actorUsuarioId, Guid actorTenantId, Guid boardId, CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var board = await _boardRepository.GetByIdAsync(boardId, actorTenantId, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("No se encontro la directiva indicada.");

        var members = await _boardRepository.GetMembersAsync(board.Id, cancellationToken: cancellationToken);
        var total = 0;

        foreach (var member in members)
        {
            var changed = await _refreshTokenRepository.RevocarTodosDelUsuarioAsync(member.UsuarioId, cancellationToken: cancellationToken);
            if (changed)
            {
                total++;
            }
        }

        return total;
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
            throw new UnauthorizedAccessException("No tienes permisos para esta accion.");
        }
    }

    private static string GenerateTemporaryPassword()
    {
        const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#";
        var random = new Random();
        var chars = new char[12];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = allowed[random.Next(allowed.Length)];
        }

        return new string(chars);
    }
}
