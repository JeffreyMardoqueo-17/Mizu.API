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
    private readonly IRolRepository _rolRepository;
    private readonly ITenantConfigRepository _tenantConfigRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MedidorService(
        IUsuarioRepository usuarioRepository,
        IMedidorRepository medidorRepository,
        IRolRepository rolRepository,
        ITenantConfigRepository tenantConfigRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _medidorRepository = medidorRepository;
        _rolRepository = rolRepository;
        _tenantConfigRepository = tenantConfigRepository;
        _refreshTokenRepository = refreshTokenRepository;
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

    public async Task<MeterTransferResponseDto> TransferirAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid medidorId,
        MeterTransferRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);
        ValidarSolicitudTransferencia(request);

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var medidorActual = await _medidorRepository.ObtenerPorIdAsync(
                    actorTenantId,
                    medidorId,
                    transaction,
                    cancellationToken);

                if (medidorActual is null)
                {
                    throw new InvalidOperationException("Contador no encontrado.");
                }

                var usuarioOrigen = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(
                    medidorActual.UsuarioId,
                    actorTenantId,
                    transaction,
                    cancellationToken);

                if (usuarioOrigen is null)
                {
                    throw new InvalidOperationException("No se encontro el titular actual de la paja.");
                }

                var usuarioDestino = await ResolverUsuarioDestinoAsync(
                    actorTenantId,
                    request,
                    transaction,
                    cancellationToken);

                if (usuarioDestino.Id == usuarioOrigen.Id)
                {
                    throw new InvalidOperationException("No puedes transferir la paja al mismo usuario titular.");
                }

                var medidorTransferido = await _medidorRepository.TransferirTitularidadAsync(
                    actorTenantId,
                    medidorId,
                    usuarioDestino.Id,
                    transaction,
                    cancellationToken);

                if (medidorTransferido is null)
                {
                    throw new InvalidOperationException("No fue posible transferir la paja.");
                }

                if (!usuarioDestino.Activo)
                {
                    await _usuarioRepository.SetActiveStateAsync(
                        usuarioDestino.Id,
                        actorTenantId,
                        true,
                        transaction,
                        cancellationToken);
                }

                var activosOrigen = await _medidorRepository.ContarActivosPorUsuarioAsync(
                    actorTenantId,
                    usuarioOrigen.Id,
                    transaction,
                    cancellationToken);

                var usuarioOrigenDesactivado = false;
                if (activosOrigen == 0 && usuarioOrigen.Activo)
                {
                    usuarioOrigenDesactivado = await _usuarioRepository.SetActiveStateAsync(
                        usuarioOrigen.Id,
                        actorTenantId,
                        false,
                        transaction,
                        cancellationToken);

                    if (usuarioOrigenDesactivado)
                    {
                        await _refreshTokenRepository.RevocarTodosDelUsuarioAsync(
                            usuarioOrigen.Id,
                            transaction,
                            cancellationToken);
                    }
                }

                var transferencia = new MedidorTransferencia
                {
                    TenantId = actorTenantId,
                    MedidorId = medidorTransferido.Id,
                    UsuarioOrigenId = usuarioOrigen.Id,
                    UsuarioDestinoId = usuarioDestino.Id,
                    TipoMovimiento = request.TipoMovimiento.Trim(),
                    Motivo = request.Motivo.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim(),
                    ReferenciaDocumento = string.IsNullOrWhiteSpace(request.ReferenciaDocumento) ? null : request.ReferenciaDocumento.Trim(),
                    ActorUsuarioId = actorUsuarioId,
                    FechaTransferencia = DateTime.UtcNow
                };

                await _medidorRepository.RegistrarTransferenciaAsync(transferencia, transaction, cancellationToken);

                var niuDestino = await _medidorRepository.ObtenerNiuUsuarioAsync(
                    actorTenantId,
                    usuarioDestino.Id,
                    transaction,
                    cancellationToken) ?? 0;

                var message = usuarioOrigenDesactivado
                    ? "Paja transferida. El usuario origen quedo desactivado por no tener pajas activas."
                    : "Paja transferida exitosamente.";

                return new MeterTransferResponseDto(
                    ToDto(medidorTransferido, niuDestino),
                    ToTransferItemDto(transferencia),
                    usuarioDestino.Id,
                    usuarioOrigenDesactivado,
                    message);
            },
            cancellationToken);
    }

    public async Task<MeterTransferHistoryResponseDto> ObtenerHistorialTransferenciasAsync(
        Guid actorUsuarioId,
        Guid actorTenantId,
        Guid medidorId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var medidor = await _medidorRepository.ObtenerPorIdAsync(actorTenantId, medidorId, cancellationToken: cancellationToken);
        if (medidor is null)
        {
            throw new InvalidOperationException("Contador no encontrado.");
        }

        var historial = await _medidorRepository.ObtenerHistorialTransferenciasAsync(
            actorTenantId,
            medidorId,
            cancellationToken: cancellationToken);

        var items = historial
            .Select(ToTransferItemDto)
            .ToList()
            .AsReadOnly();

        return new MeterTransferHistoryResponseDto(medidorId, items, items.Count);
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

    private static MeterTransferItemDto ToTransferItemDto(MedidorTransferencia transferencia)
    {
        return new MeterTransferItemDto(
            transferencia.Id,
            transferencia.MedidorId,
            transferencia.UsuarioOrigenId,
            transferencia.UsuarioDestinoId,
            transferencia.TipoMovimiento,
            transferencia.Motivo,
            transferencia.Observaciones,
            transferencia.ReferenciaDocumento,
            transferencia.ActorUsuarioId,
            transferencia.FechaTransferencia);
    }

    private async Task<Usuario> ResolverUsuarioDestinoAsync(
        Guid tenantId,
        MeterTransferRequestDto request,
        System.Data.IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (request.DestinoUsuarioId.HasValue)
        {
            var destinoExistente = await _usuarioRepository.ObtenerPorIdYTenantIncluyendoInactivosAsync(
                request.DestinoUsuarioId.Value,
                tenantId,
                transaction,
                cancellationToken);

            if (destinoExistente is null)
            {
                throw new InvalidOperationException("El usuario destino no existe en este tenant.");
            }

            return destinoExistente;
        }

        if (request.NuevoUsuario is null)
        {
            throw new InvalidOperationException("Debes indicar usuario destino o datos del nuevo usuario.");
        }

        var correo = request.NuevoUsuario.Correo.Trim();
        var existenteCorreo = await _usuarioRepository.ObtenerPorCorreoAsync(correo, transaction, cancellationToken);
        if (existenteCorreo is not null)
        {
            throw new InvalidOperationException("Ya existe un usuario con el correo indicado.");
        }

        var rolSocio = await _rolRepository.ObtenerPorNombreAsync(Muzu.Api.Core.Rules.SystemRoles.Socio, transaction, cancellationToken)
            ?? throw new InvalidOperationException("No se encontro el rol Socio para crear el nuevo titular.");

        var nuevoUsuario = new Usuario
        {
            TenantId = tenantId,
            Nombre = request.NuevoUsuario.Nombre.Trim(),
            Apellido = request.NuevoUsuario.Apellido.Trim(),
            DUI = request.NuevoUsuario.DUI.Trim(),
            Correo = correo,
            Telefono = request.NuevoUsuario.Telefono.Trim(),
            Direccion = request.NuevoUsuario.Direccion.Trim(),
            PasswordHash = string.Empty,
            Rol = rolSocio.Nombre,
            Activo = true,
            Eliminado = false,
            MustChangePassword = false,
            FechaActualizacion = DateTime.UtcNow,
            TempPasswordGeneratedAt = null,
            TempPasswordViewedAt = null
        };

        await _usuarioRepository.CrearUsuarioAsync(nuevoUsuario, transaction, cancellationToken);
        await _usuarioRepository.AsignarRolAsync(nuevoUsuario.Id, rolSocio.Id, rolSocio.Nombre, transaction, cancellationToken);
        return nuevoUsuario;
    }

    private static void ValidarSolicitudTransferencia(MeterTransferRequestDto request)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Solicitud de transferencia invalida.");
        }

        var tieneDestino = request.DestinoUsuarioId.HasValue;
        var tieneNuevoUsuario = request.NuevoUsuario is not null;
        if (tieneDestino == tieneNuevoUsuario)
        {
            throw new InvalidOperationException("Debes enviar solo un destino: usuario existente o nuevo usuario.");
        }

        if (string.IsNullOrWhiteSpace(request.TipoMovimiento))
        {
            throw new InvalidOperationException("El tipo de movimiento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new InvalidOperationException("El motivo de transferencia es obligatorio.");
        }

        if (request.NuevoUsuario is not null)
        {
            if (string.IsNullOrWhiteSpace(request.NuevoUsuario.Nombre)
                || string.IsNullOrWhiteSpace(request.NuevoUsuario.Apellido)
                || string.IsNullOrWhiteSpace(request.NuevoUsuario.DUI)
                || string.IsNullOrWhiteSpace(request.NuevoUsuario.Correo)
                || string.IsNullOrWhiteSpace(request.NuevoUsuario.Telefono)
                || string.IsNullOrWhiteSpace(request.NuevoUsuario.Direccion))
            {
                throw new InvalidOperationException("Para crear nuevo titular debes completar nombre, apellido, DUI, correo, telefono y direccion.");
            }
        }
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
