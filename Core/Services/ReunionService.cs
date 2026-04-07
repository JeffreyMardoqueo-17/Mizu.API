using System.Text.Json;
using Muzu.Api.Core.DTOs;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Interfaces.Service;
using Muzu.Api.Core.Models;
using Muzu.Api.Core.Rules;

namespace Muzu.Api.Core.Services;

public sealed class ReunionService : IReunionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReunionRepository _reunionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITenantConfigRepository _tenantConfigRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReunionService(
        IReunionRepository reunionRepository,
        IUsuarioRepository usuarioRepository,
        ITenantConfigRepository tenantConfigRepository,
        IBillingRepository billingRepository,
        IUnitOfWork unitOfWork)
    {
        _reunionRepository = reunionRepository;
        _usuarioRepository = usuarioRepository;
        _tenantConfigRepository = tenantConfigRepository;
        _billingRepository = billingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ReunionListItemDto>> ListarAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var reuniones = await _reunionRepository.ListarPorTenantAsync(actorTenantId, cancellationToken: cancellationToken);
        var activeUsers = await _usuarioRepository.ListarActivosAsync(actorTenantId, cancellationToken: cancellationToken);
        var activeUserIds = activeUsers.Select(item => item.Id).ToHashSet();
        var items = new List<ReunionListItemDto>();

        foreach (var reunion in reuniones)
        {
            var asistencias = await _reunionRepository.ObtenerAsistenciasAsync(reunion.Id, cancellationToken: cancellationToken);
            var totalSocios = activeUserIds.Count;
            var totalPresentes = asistencias.Count(item => item.Asistio && activeUserIds.Contains(item.UsuarioId));
            items.Add(new ReunionListItemDto(
                reunion.Id,
                reunion.Titulo,
                reunion.FechaReunion,
                reunion.HoraInicio,
                reunion.HoraFin,
                reunion.Estado,
                totalSocios,
                totalPresentes,
                totalSocios - totalPresentes,
                reunion.FechaCreacion,
                reunion.FinalizadaAt));
        }

        return items;
    }

    public async Task<ReunionDetailDto?> ObtenerPorIdAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var reunion = await _reunionRepository.ObtenerPorIdAsync(actorTenantId, reunionId, cancellationToken: cancellationToken);
        return reunion is null ? null : await BuildDetailAsync(reunion, cancellationToken);
    }

    public async Task<ReunionDetailDto> CrearAsync(Guid actorUsuarioId, Guid actorTenantId, CreateReunionRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        var title = request.Titulo.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("El titulo de la reunion es obligatorio.");
        }

        var puntos = NormalizeItems(request.PuntosTratar);
        if (puntos.Count == 0)
        {
            throw new InvalidOperationException("Debes incluir al menos un punto a tratar.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var reunion = new Reunion
            {
                TenantId = actorTenantId,
                Titulo = title,
                FechaReunion = request.FechaReunion,
                HoraInicio = request.HoraInicio,
                Estado = "Programada",
                PuntosTratarJson = JsonSerializer.Serialize(puntos, JsonOptions),
                AcuerdosJson = JsonSerializer.Serialize(Array.Empty<string>(), JsonOptions),
                CreadoPorUsuarioId = actorUsuarioId,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            };

            await _reunionRepository.CrearAsync(reunion, transaction, cancellationToken);

            var usuariosActivos = await _usuarioRepository.ListarActivosAsync(actorTenantId, transaction, cancellationToken);
            foreach (var usuario in usuariosActivos)
            {
                await _reunionRepository.CrearAsistenciaAsync(new ReunionAsistencia
                {
                    ReunionId = reunion.Id,
                    UsuarioId = usuario.Id,
                    Asistio = false,
                    Observacion = null,
                    MarcadoPorUsuarioId = null,
                    FechaMarcado = null,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                }, transaction, cancellationToken);
            }

            await _reunionRepository.AgregarHistorialAsync(new ReunionHistorial
            {
                ReunionId = reunion.Id,
                Evento = "REUNION_CREADA",
                Descripcion = $"Reunion '{reunion.Titulo}' creada con {puntos.Count} punto(s) a tratar.",
                ActorUsuarioId = actorUsuarioId,
                FechaCreacion = DateTime.UtcNow
            }, transaction, cancellationToken);

            return await BuildDetailAsync(reunion, transaction, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ReunionDetailDto?> ActualizarAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, UpdateReunionRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var reunion = await _reunionRepository.ObtenerPorIdAsync(actorTenantId, reunionId, transaction, cancellationToken);
            if (reunion is null)
            {
                return null;
            }

            EnsureNotFinalized(reunion);

            var puntos = NormalizeItems(request.PuntosTratar);
            if (puntos.Count == 0)
            {
                throw new InvalidOperationException("Debes incluir al menos un punto a tratar.");
            }

            reunion.Titulo = request.Titulo.Trim();
            reunion.FechaReunion = request.FechaReunion;
            reunion.HoraInicio = request.HoraInicio;
            reunion.HoraFin = request.HoraFin;
            reunion.PuntosTratarJson = JsonSerializer.Serialize(puntos, JsonOptions);
            reunion.FechaActualizacion = DateTime.UtcNow;

            var updated = await _reunionRepository.ActualizarAsync(reunion, transaction, cancellationToken);
            if (!updated)
            {
                return null;
            }

            await _reunionRepository.AgregarHistorialAsync(new ReunionHistorial
            {
                ReunionId = reunion.Id,
                Evento = "REUNION_ACTUALIZADA",
                Descripcion = "Se actualizaron titulo, fecha, hora o puntos de la reunion.",
                ActorUsuarioId = actorUsuarioId,
                FechaCreacion = DateTime.UtcNow
            }, transaction, cancellationToken);

            return await BuildDetailAsync(reunion, transaction, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ReunionDetailDto?> IniciarAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var reunion = await _reunionRepository.ObtenerPorIdAsync(actorTenantId, reunionId, transaction, cancellationToken);
            if (reunion is null)
            {
                return null;
            }

            EnsureNotFinalized(reunion);
            EnsureMeetingStarted(reunion, "No puedes iniciar la reunion antes de la fecha y hora programadas.");

            if (!string.Equals(reunion.Estado, "EnCurso", StringComparison.OrdinalIgnoreCase))
            {
                reunion.Estado = "EnCurso";
                reunion.IniciadaAt = DateTime.UtcNow;
                reunion.FechaActualizacion = DateTime.UtcNow;

                await _reunionRepository.CambiarEstadoAsync(reunion.Id, actorTenantId, reunion.Estado, reunion.IniciadaAt, null, null, transaction, cancellationToken);
                await _reunionRepository.AgregarHistorialAsync(new ReunionHistorial
                {
                    ReunionId = reunion.Id,
                    Evento = "REUNION_INICIADA",
                    Descripcion = "La reunion fue iniciada y quedo habilitada para pasar lista.",
                    ActorUsuarioId = actorUsuarioId,
                    FechaCreacion = DateTime.UtcNow
                }, transaction, cancellationToken);
            }

            return await BuildDetailAsync(reunion, transaction, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ReunionDetailDto?> ActualizarAsistenciaAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, UpdateReunionAsistenciaRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var reunion = await _reunionRepository.ObtenerPorIdAsync(actorTenantId, reunionId, transaction, cancellationToken);
            if (reunion is null)
            {
                return null;
            }

            EnsureNotFinalized(reunion);
            EnsureMeetingStarted(reunion, "No puedes pasar lista antes de la fecha y hora de inicio de la reunion.");

            if (!string.Equals(reunion.Estado, "EnCurso", StringComparison.OrdinalIgnoreCase))
            {
                reunion.Estado = "EnCurso";
                reunion.IniciadaAt = reunion.IniciadaAt ?? DateTime.UtcNow;
                await _reunionRepository.CambiarEstadoAsync(reunion.Id, actorTenantId, reunion.Estado, reunion.IniciadaAt, null, null, transaction, cancellationToken);
            }

            foreach (var item in request.Asistencias)
            {
                var ok = await _reunionRepository.ActualizarAsistenciaAsync(reunion.Id, item.UsuarioId, item.Asistio, item.Observacion?.Trim(), actorUsuarioId, transaction, cancellationToken);
                if (!ok)
                {
                    throw new InvalidOperationException("No se pudo actualizar la asistencia de uno de los socios.");
                }
            }

            await _reunionRepository.AgregarHistorialAsync(new ReunionHistorial
            {
                ReunionId = reunion.Id,
                Evento = "ASISTENCIA_ACTUALIZADA",
                Descripcion = $"Se actualizo la asistencia de {request.Asistencias.Count} participante(s).",
                ActorUsuarioId = actorUsuarioId,
                FechaCreacion = DateTime.UtcNow
            }, transaction, cancellationToken);

            return await BuildDetailAsync(reunion, transaction, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ReunionDetailDto?> ActualizarAcuerdosAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, UpdateReunionAcuerdosRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var reunion = await _reunionRepository.ObtenerPorIdAsync(actorTenantId, reunionId, transaction, cancellationToken);
            if (reunion is null)
            {
                return null;
            }

            EnsureNotFinalized(reunion);

            var acuerdos = NormalizeItems(request.Acuerdos);
            reunion.AcuerdosJson = JsonSerializer.Serialize(acuerdos, JsonOptions);
            reunion.NotasSecretaria = string.IsNullOrWhiteSpace(request.NotasSecretaria) ? null : request.NotasSecretaria.Trim();
            reunion.FechaActualizacion = DateTime.UtcNow;

            var ok = await _reunionRepository.ActualizarAcuerdosAsync(reunion.Id, actorTenantId, reunion.AcuerdosJson!, reunion.NotasSecretaria, transaction, cancellationToken);
            if (!ok)
            {
                return null;
            }

            await _reunionRepository.AgregarHistorialAsync(new ReunionHistorial
            {
                ReunionId = reunion.Id,
                Evento = "ACUERDOS_ACTUALIZADOS",
                Descripcion = $"Se guardaron {acuerdos.Count} acuerdo(s) y notas de secretaria.",
                ActorUsuarioId = actorUsuarioId,
                FechaCreacion = DateTime.UtcNow
            }, transaction, cancellationToken);

            return await BuildDetailAsync(reunion, transaction, cancellationToken);
        }, cancellationToken);
    }

    public async Task<ReunionDetailDto?> FinalizarAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, FinalizeReunionRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorUsuarioId, actorTenantId, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var reunion = await _reunionRepository.ObtenerPorIdAsync(actorTenantId, reunionId, transaction, cancellationToken);
            if (reunion is null)
            {
                return null;
            }

            EnsureNotFinalized(reunion);
            EnsureMeetingStarted(reunion, "No puedes finalizar la reunion antes de la fecha y hora de inicio programadas.");

            reunion.Estado = "Finalizada";
            reunion.HoraFin = request.HoraFin ?? TimeOnly.FromDateTime(DateTime.Now);
            reunion.FinalizadaAt = DateTime.UtcNow;
            reunion.FechaActualizacion = DateTime.UtcNow;

            var config = await _tenantConfigRepository.ObtenerPorTenantIdAsync(actorTenantId, transaction, cancellationToken)
                ?? throw new InvalidOperationException("El tenant no tiene configuracion de multas.");

            if (!await _reunionRepository.CambiarEstadoAsync(reunion.Id, actorTenantId, reunion.Estado, reunion.IniciadaAt, reunion.FinalizadaAt, reunion.HoraFin, transaction, cancellationToken))
            {
                return null;
            }

            var asistencias = await _reunionRepository.ObtenerAsistenciasAsync(reunion.Id, transaction, cancellationToken);
            var absentIds = asistencias
                .Where(item => !item.Asistio)
                .Select(item => item.UsuarioId)
                .ToHashSet();

            var activeUsers = await _usuarioRepository.ListarActivosAsync(actorTenantId, transaction, cancellationToken);
            var activeUserIds = activeUsers.Select(item => item.Id).ToHashSet();
            var penalizados = 0;

            foreach (var socioId in absentIds.Intersect(activeUserIds))
            {
                await _billingRepository.RegistrarMultaOperativaPendienteAsync(new OperationalPenalty
                {
                    TenantId = actorTenantId,
                    UsuarioId = socioId,
                    SourceType = "reunion",
                    SourceDate = reunion.FechaReunion,
                    Amount = config.MultaNoAsistirReunion,
                    Status = "pendiente",
                    AssignmentStrategy = "primary_meter",
                    Notes = $"Inasistencia a reunion: {reunion.Titulo}",
                    CreatedBy = actorUsuarioId,
                    CreatedAt = DateTime.UtcNow
                }, transaction, cancellationToken);

                penalizados++;
            }

            await _reunionRepository.AgregarHistorialAsync(new ReunionHistorial
            {
                ReunionId = reunion.Id,
                Evento = "REUNION_FINALIZADA",
                Descripcion = $"La reunion se finalizo y se generaron {penalizados} multa(s) por inasistencia.",
                ActorUsuarioId = actorUsuarioId,
                FechaCreacion = DateTime.UtcNow
            }, transaction, cancellationToken);

            return await BuildDetailAsync(reunion, transaction, cancellationToken);
        }, cancellationToken);
    }

    private async Task EnsureManagerAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken)
    {
        var actor = await _usuarioRepository.ObtenerPorIdYTenantAsync(actorUsuarioId, actorTenantId, cancellationToken: cancellationToken);
        if (actor is null)
        {
            throw new UnauthorizedAccessException("Usuario autenticado invalido.");
        }

        if (!SystemRoles.EsAdministrador(actor.Rol)
            && !string.Equals(actor.Rol, SystemRoles.Presidente, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(actor.Rol, SystemRoles.Secretario, StringComparison.OrdinalIgnoreCase)
            && !SystemRoles.EsRolDeDirectiva(actor.Rol))
        {
            throw new UnauthorizedAccessException("No tienes permisos para administrar reuniones.");
        }
    }

    private static void EnsureNotFinalized(Reunion reunion)
    {
        if (string.Equals(reunion.Estado, "Finalizada", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La reunion ya fue finalizada y no se puede editar.");
        }
    }

    private static void EnsureMeetingStarted(Reunion reunion, string message)
    {
        var startAt = reunion.FechaReunion.ToDateTime(reunion.HoraInicio);
        if (DateTime.Now < startAt)
        {
            throw new InvalidOperationException(message);
        }
    }

    private async Task<ReunionDetailDto> BuildDetailAsync(Reunion reunion, CancellationToken cancellationToken)
    {
        return await BuildDetailAsync(reunion, null, cancellationToken);
    }

    private async Task<ReunionDetailDto> BuildDetailAsync(Reunion reunion, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        await EnsureAttendanceRowsForActiveUsersAsync(reunion, transaction, cancellationToken);

        var activeUsers = await _usuarioRepository.ListarActivosAsync(reunion.TenantId, transaction, cancellationToken);
        var activeUserLookup = activeUsers.ToDictionary(item => item.Id, item => item);
        var asistencias = await _reunionRepository.ObtenerAsistenciasAsync(reunion.Id, transaction, cancellationToken);
        var historial = await _reunionRepository.ObtenerHistorialAsync(reunion.Id, transaction, cancellationToken);
        var asistenciaPorUsuario = asistencias.ToDictionary(item => item.UsuarioId, item => item);

        var attendanceDtos = new List<ReunionAttendanceDto>(activeUsers.Count);
        foreach (var usuario in activeUsers)
        {
            asistenciaPorUsuario.TryGetValue(usuario.Id, out var asistencia);

            attendanceDtos.Add(new ReunionAttendanceDto(
                usuario.Id,
                $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                usuario.DUI,
                usuario.Rol,
                asistencia?.Asistio ?? false,
                asistencia?.Observacion,
                asistencia?.MarcadoPorUsuarioId,
                asistencia?.FechaMarcado));
        }

        var historyDtos = historial.Select(item => new ReunionHistoryItemDto(item.Id, item.ReunionId, item.Evento, item.Descripcion, item.ActorUsuarioId, item.FechaCreacion)).ToList();

        return new ReunionDetailDto(
            reunion.Id,
            reunion.TenantId,
            reunion.Titulo,
            reunion.FechaReunion,
            reunion.HoraInicio,
            reunion.HoraFin,
            reunion.Estado,
            DeserializeList(reunion.PuntosTratarJson),
            DeserializeList(reunion.AcuerdosJson),
            reunion.NotasSecretaria,
            reunion.CreadoPorUsuarioId,
            reunion.IniciadaAt,
            reunion.FinalizadaAt,
            reunion.FechaCreacion,
            reunion.FechaActualizacion,
            attendanceDtos,
            historyDtos);
    }

    private async Task EnsureAttendanceRowsForActiveUsersAsync(Reunion reunion, System.Data.IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var activeUsers = await _usuarioRepository.ListarActivosAsync(reunion.TenantId, transaction, cancellationToken);
        var asistencias = await _reunionRepository.ObtenerAsistenciasAsync(reunion.Id, transaction, cancellationToken);
        var existingUserIds = asistencias.Select(item => item.UsuarioId).ToHashSet();

        foreach (var usuario in activeUsers)
        {
            if (existingUserIds.Contains(usuario.Id))
            {
                continue;
            }

            await _reunionRepository.CrearAsistenciaAsync(new ReunionAsistencia
            {
                ReunionId = reunion.Id,
                UsuarioId = usuario.Id,
                Asistio = false,
                Observacion = null,
                MarcadoPorUsuarioId = null,
                FechaMarcado = null,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            }, transaction, cancellationToken);
        }
    }

    private static IReadOnlyList<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<string> NormalizeItems(IReadOnlyList<string> items)
    {
        return items
            .Select(item => item?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}