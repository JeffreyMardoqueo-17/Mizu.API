using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class MedidorService : IMedidorService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IMedidorRepository _medidorRepository;
    private readonly ITenantConfigRepository _tenantConfigRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MedidorService(
        IUsuarioRepository usuarioRepository,
        IMedidorRepository medidorRepository,
        ITenantConfigRepository tenantConfigRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _medidorRepository = medidorRepository;
        _tenantConfigRepository = tenantConfigRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MeterListResponseDto> ObtenerPorUsuarioAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var usuario = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(usuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (usuario is null)
        {
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        var niu = await _medidorRepository.ObtenerNiuUsuarioAsync(actorTenantId, usuarioId, cancellationToken: cancellationToken)
            ?? 0;

        var medidores = await _medidorRepository.ObtenerPorUsuarioAsync(actorTenantId, usuarioId, cancellationToken: cancellationToken);
        var (permitirMultiples, maximoContadores) = await ObtenerReglasContadoresAsync(actorTenantId, cancellationToken);
        var items = medidores
            .Select(m => ToDto(m, niu))
            .ToList()
            .AsReadOnly();

        var totalActivos = items.Count(x => x.Activo);
        var maximoActivosPermitidos = permitirMultiples ? maximoContadores : 1;
        var excedeReglaActivos = totalActivos > maximoActivosPermitidos;
        var puedeAgregarOtro = totalActivos < maximoActivosPermitidos;

        return new MeterListResponseDto(
            usuarioId,
            niu,
            items,
            items.Count,
            totalActivos,
            permitirMultiples,
            maximoContadores,
            maximoActivosPermitidos,
            excedeReglaActivos,
            puedeAgregarOtro);
    }

    public async Task<MeterNextNumberResponseDto> ObtenerSiguienteNumeroAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var usuario = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(usuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (usuario is null)
        {
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        var (niu, nextNumber) = await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var resolvedNiu = await _medidorRepository.AsignarNiuCorrelativoSiFaltaAsync(
                    actorTenantId,
                    usuarioId,
                    transaction,
                    cancellationToken);

                var next = await _medidorRepository.ObtenerSiguienteNumeroMedidorAsync(
                    actorTenantId,
                    transaction,
                    cancellationToken);

                return (resolvedNiu, next);
            },
            cancellationToken);

        return new MeterNextNumberResponseDto(usuarioId, niu, nextNumber);
    }

    public async Task<MeterAssignResponseDto> AsignarAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        MeterAssignRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var usuario = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(request.UsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (usuario is null)
        {
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        var (niu, medidor) = await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var (permitirMultiples, maximoContadores) = await ObtenerReglasContadoresAsync(actorTenantId, cancellationToken);
                var totalActivos = await _medidorRepository.ContarActivosPorUsuarioAsync(
                    actorTenantId,
                    request.UsuarioId,
                    transaction,
                    cancellationToken);

                if (!permitirMultiples && totalActivos >= 1)
                {
                    throw new InvalidOperationException("La configuracion actual no permite multiples contadores por usuario.");
                }

                if (totalActivos >= maximoContadores)
                {
                    throw new InvalidOperationException($"Este usuario ya alcanzo el maximo permitido de {maximoContadores} contador(es).");
                }

                var resolvedNiu = await _medidorRepository.AsignarNiuCorrelativoSiFaltaAsync(
                    actorTenantId,
                    request.UsuarioId,
                    transaction,
                    cancellationToken);

                var nextNumber = await _medidorRepository.ObtenerSiguienteNumeroMedidorAsync(
                    actorTenantId,
                    transaction,
                    cancellationToken);

                var nuevo = new Medidor
                {
                    TenantId = actorTenantId,
                    UsuarioId = request.UsuarioId,
                    NumeroMedidor = nextNumber,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow,
                    Eliminado = false
                };

                var created = await _medidorRepository.CrearAsync(nuevo, transaction, cancellationToken);
                return (resolvedNiu, created);
            },
            cancellationToken);

        return new MeterAssignResponseDto(
            ToDto(medidor, niu),
            "Contador asignado exitosamente.");
    }

    public async Task<MeterStatusResponseDto> ActualizarEstadoAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid medidorId,
        MeterStatusUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var medidorActual = await _medidorRepository.ObtenerPorIdAsync(
            actorTenantId,
            medidorId,
            cancellationToken: cancellationToken);

        if (medidorActual is null)
        {
            throw new InvalidOperationException("Contador no encontrado.");
        }

        if (request.Activo && !medidorActual.Activo)
        {
            var (permitirMultiples, maximoContadores) = await ObtenerReglasContadoresAsync(actorTenantId, cancellationToken);
            var totalActivos = await _medidorRepository.ContarActivosPorUsuarioAsync(
                actorTenantId,
                medidorActual.UsuarioId,
                cancellationToken: cancellationToken);

            if (!permitirMultiples && totalActivos >= 1)
            {
                throw new InvalidOperationException("No puedes activar este contador porque la regla actual permite solo un contador activo por usuario.");
            }

            if (totalActivos >= maximoContadores)
            {
                throw new InvalidOperationException($"No puedes activar este contador porque el usuario ya tiene {totalActivos} activo(s) y el maximo permitido es {maximoContadores}.");
            }
        }

        var medidorActualizado = await _medidorRepository.ActualizarEstadoAsync(
            actorTenantId,
            medidorId,
            request.Activo,
            cancellationToken: cancellationToken);

        if (medidorActualizado is null)
        {
            throw new InvalidOperationException("Contador no encontrado.");
        }

        var niu = await _medidorRepository.ObtenerNiuUsuarioAsync(
            actorTenantId,
            medidorActualizado.UsuarioId,
            cancellationToken: cancellationToken) ?? 0;

        var message = request.Activo
            ? "Contador activado exitosamente."
            : "Contador desactivado exitosamente.";

        return new MeterStatusResponseDto(ToDto(medidorActualizado, niu), message);
    }

    public async Task<MeterRuleConflictReportDto> ObtenerReporteConflictosActivosAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var (permitirMultiples, maximoContadores) = await ObtenerReglasContadoresAsync(actorTenantId, cancellationToken);
        var maximoActivosPermitidos = permitirMultiples ? maximoContadores : 1;

        var conflictos = await _medidorRepository.ObtenerUsuariosConExcesoActivosAsync(
            actorTenantId,
            maximoActivosPermitidos,
            cancellationToken: cancellationToken);

        var items = conflictos
            .Select(row => new MeterRuleConflictItemDto(
                row.UsuarioId,
                row.Nombre,
                row.Apellido,
                row.Correo,
                row.TotalActivos,
                row.NumerosMedidoresActivos))
            .ToList()
            .AsReadOnly();

        return new MeterRuleConflictReportDto(
            permitirMultiples,
            maximoActivosPermitidos,
            items,
            items.Count);
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
            throw new UnauthorizedAccessException("No tienes permisos para administrar contadores.");
        }
    }

    private static MeterDto ToDto(Medidor medidor, long niu)
    {
        return new MeterDto(
            medidor.Id,
            medidor.TenantId,
            medidor.UsuarioId,
            niu,
            medidor.NumeroMedidor,
            medidor.Activo,
            medidor.FechaCreacion,
            medidor.FechaActualizacion);
    }

    private async Task<(bool PermitirMultiples, int Maximo)> ObtenerReglasContadoresAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var config = await _tenantConfigRepository.ObtenerPorTenantIdAsync(tenantId, cancellationToken: cancellationToken);
        if (config is null)
        {
            return (false, 1);
        }

        var maximo = config.MaximoContadoresPorUsuario < 1 ? 1 : config.MaximoContadoresPorUsuario;
        var permitirMultiples = config.PermitirMultiplesContadores;

        if (!permitirMultiples)
        {
            return (false, 1);
        }

        return (true, maximo);
    }
}
