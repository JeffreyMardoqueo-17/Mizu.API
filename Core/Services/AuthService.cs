using System.Data;
using System.Security.Cryptography;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Mappers;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class AuthService : IAuthService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITenantConfigRepository _tenantConfigRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        ITenantRepository tenantRepository,
        IUsuarioRepository usuarioRepository,
        ITenantConfigRepository tenantConfigRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _usuarioRepository = usuarioRepository;
        _tenantConfigRepository = tenantConfigRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterTenantCommandResultDto> RegistrarPrimerTenantYUsuarioAsync(TenantUsuarioRegistroDto request, CancellationToken cancellationToken = default)
    {
        var tenantExistente = await _tenantRepository.ObtenerPorNombreAsync(request.Tenant.Nombre, cancellationToken: cancellationToken);
        if (tenantExistente is not null)
        {
            throw new InvalidOperationException("Ya existe un tenant con ese nombre.");
        }

        var usuarioExistente = await _usuarioRepository.ObtenerPorCorreoAsync(request.Usuario.Correo, cancellationToken: cancellationToken);
        if (usuarioExistente is not null)
        {
            throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var tenant = request.Tenant.ToEntity();
                await _tenantRepository.CrearTenantAsync(tenant, transaction, cancellationToken);

                var config = new TenantConfig
                {
                    TenantId = tenant.Id
                };
                await _tenantConfigRepository.CrearConfigAsync(config, transaction, cancellationToken);

                var passwordHash = PasswordHasher.HashPassword(request.Usuario.Password);
                var usuario = request.Usuario.ToEntity(tenant.Id, passwordHash);
                await _usuarioRepository.CrearUsuarioAsync(usuario, transaction, cancellationToken);

                var authResult = await GenerarAutenticacionAsync(usuario, transaction, cancellationToken);
                var configDto = config.ToResponseDto();
                var response = new RegisterTenantResponseDto(
                    usuario.ToResumenDto(),
                    tenant.ToResumenDto(configDto));

                return new RegisterTenantCommandResultDto(
                    authResult.AccessToken,
                    authResult.Response.RefreshToken,
                    response);
            },
            cancellationToken);
    }

    public async Task<AuthenticatedCommandResultDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(loginDto.Correo, cancellationToken: cancellationToken);
        if (usuario is null || !PasswordHasher.VerifyPassword(loginDto.Password, usuario.PasswordHash))
        {
            return null;
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            transaction => GenerarAutenticacionAsync(usuario, transaction, cancellationToken),
            cancellationToken);
    }

    public async Task<AuthenticatedCommandResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var tokenExistente = await _refreshTokenRepository.ObtenerPorTokenAsync(refreshToken, transaction, cancellationToken);
                if (tokenExistente is null || tokenExistente.Revocado || tokenExistente.Expira <= DateTime.UtcNow)
                {
                    return null;
                }

                var usuario = await _usuarioRepository.ObtenerPorIdAsync(tokenExistente.UsuarioId, transaction, cancellationToken);
                if (usuario is null)
                {
                    return null;
                }

                await _refreshTokenRepository.RevocarAsync(refreshToken, transaction, cancellationToken);
                return await GenerarAutenticacionAsync(usuario, transaction, cancellationToken);
            },
            cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        await _refreshTokenRepository.RevocarAsync(refreshToken.Trim(), cancellationToken: cancellationToken);
    }

    private async Task<AuthenticatedCommandResultDto> GenerarAutenticacionAsync(Usuario usuario, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var accessToken = _jwtService.GenerarToken(usuario.Id, usuario.TenantId, usuario.Rol);

        var refreshToken = new RefreshToken
        {
            UsuarioId = usuario.Id,
            Token = GenerarRefreshToken(),
            Expira = DateTime.UtcNow.AddHours(24)
        };

        await _refreshTokenRepository.CrearAsync(refreshToken, transaction, cancellationToken);

        return new AuthenticatedCommandResultDto(
            accessToken,
            new LoginResponseDto(
                usuario.TenantId,
                usuario.Id,
                usuario.Rol,
                refreshToken.Token));
    }

    private static string GenerarRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
