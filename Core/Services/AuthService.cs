using System.Data;
using System.Security.Cryptography;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Mappers;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;
using System.Text.Json;

namespace Muzu.Api.Core.Services;

public sealed class AuthService : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ITenantRepository _tenantRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITenantConfigRepository _tenantConfigRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRolRepository _rolRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        ITenantRepository tenantRepository,
        IUsuarioRepository usuarioRepository,
        ITenantConfigRepository tenantConfigRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IRolRepository rolRepository,
        IBoardRepository boardRepository,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _usuarioRepository = usuarioRepository;
        _tenantConfigRepository = tenantConfigRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _rolRepository = rolRepository;
        _boardRepository = boardRepository;
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

                var config = BuildInitialConfig(tenant.Id, request.ConfiguracionInicial);
                await _tenantConfigRepository.CrearConfigAsync(config, transaction, cancellationToken);

                var passwordHash = PasswordHasher.HashPassword(request.Usuario.Password);
                var rolAdministrador = await _rolRepository.ObtenerPorNombreAsync(SystemRoles.Administrador, transaction, cancellationToken)
                    ?? throw new InvalidOperationException("No se encontro el rol Administrador en el catalogo de roles.");

                var usuario = request.Usuario.ToEntity(tenant.Id, passwordHash, rolAdministrador.Nombre);
                await _usuarioRepository.CrearUsuarioAsync(usuario, transaction, cancellationToken);
                await _usuarioRepository.AsignarRolAsync(usuario.Id, rolAdministrador.Id, rolAdministrador.Nombre, transaction, cancellationToken);

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

        await ValidarAccesoPorDirectivaAsync(usuario, cancellationToken);

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

                await ValidarAccesoPorDirectivaAsync(usuario, cancellationToken);

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
                usuario.Nombre,
                usuario.Apellido,
                usuario.Rol,
                refreshToken.Token,
                usuario.MustChangePassword));
    }

    private static string GenerarRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static TenantConfig BuildInitialConfig(Guid tenantId, InitialTenantConfigDto? initial)
    {
        var config = new TenantConfig
        {
            TenantId = tenantId,
            Moneda = string.IsNullOrWhiteSpace(initial?.Moneda) ? "USD" : initial.Moneda.Trim().ToUpperInvariant(),
            LimiteConsumoFijo = initial?.LimiteConsumoFijo ?? 35,
            PrecioConsumoFijo = initial?.PrecioConsumoFijo ?? 3,
            MultaRetraso = initial?.MultaRetraso ?? 2,
            MultaNoAsistirReunion = initial?.MultaNoAsistirReunion ?? 5,
            MultaNoAsistirTrabajo = initial?.MultaNoAsistirTrabajo ?? 10,
        };

        var tramos = NormalizeOrDefaultTramos(initial?.TramosConsumo, config.LimiteConsumoFijo);
        config.TramosConsumoJson = JsonSerializer.Serialize(tramos, JsonOptions);

        var tramosFijos = tramos
            .Where(t => t.ModoCobro == "fijo_por_rango" && t.HastaM3.HasValue)
            .OrderBy(t => t.DesdeM3)
            .ToList();

        var porM3 = tramos
            .FirstOrDefault(t => t.ModoCobro == "por_m3");

        config.LimiteConsumoExtra1 = tramosFijos.ElementAtOrDefault(0)?.HastaM3 ?? 45;
        config.CargoExtra1 = tramosFijos.ElementAtOrDefault(0)?.Cargo ?? 0.50m;
        config.LimiteConsumoExtra2 = tramosFijos.ElementAtOrDefault(1)?.HastaM3 ?? 55;
        config.CargoExtra2 = tramosFijos.ElementAtOrDefault(1)?.Cargo ?? 0.50m;
        config.LimiteConsumoExtra3 = tramosFijos.ElementAtOrDefault(2)?.HastaM3 ?? 65;
        config.CargoExtra3 = tramosFijos.ElementAtOrDefault(2)?.Cargo ?? 0.50m;
        config.CargoExcesoMayor = porM3?.Cargo ?? 1.00m;

        return config;
    }

    private static IReadOnlyList<ConsumoTramoDto> NormalizeOrDefaultTramos(IReadOnlyList<ConsumoTramoDto>? tramos, decimal limiteConsumoFijo)
    {
        if (tramos is null || tramos.Count == 0)
        {
            return new List<ConsumoTramoDto>
            {
                new(limiteConsumoFijo, 45, 0.50m, "fijo_por_rango"),
                new(45, 55, 0.50m, "fijo_por_rango"),
                new(55, 65, 0.50m, "fijo_por_rango"),
                new(65, null, 1.00m, "por_m3")
            };
        }

        return tramos
            .Select(t => new ConsumoTramoDto(t.DesdeM3, t.HastaM3, t.Cargo, t.ModoCobro.Trim().ToLowerInvariant()))
            .OrderBy(t => t.DesdeM3)
            .ToList();
    }

    private async Task ValidarAccesoPorDirectivaAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        if (SystemRoles.EsAdministrador(usuario.Rol))
        {
            return;
        }

        if (!SystemRoles.EsRolDeDirectiva(usuario.Rol))
        {
            return;
        }

        var perteneceADirectivaActiva = await _boardRepository.IsUserInActiveBoardAsync(usuario.TenantId, usuario.Id, cancellationToken: cancellationToken);
        if (!perteneceADirectivaActiva)
        {
            throw new DirectivaAccessBlockedException("Tu cuenta no puede ingresar aun. Debes esperar que el administrador active la directiva.");
        }
    }
}
