using Muzu.Api.Core.DTOs;

namespace Muzu.Api.Core.Interfaces.Service;

public interface IReunionService
{
    Task<IReadOnlyList<ReunionListItemDto>> ListarAsync(Guid actorUsuarioId, Guid actorTenantId, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto?> ObtenerPorIdAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto> CrearAsync(Guid actorUsuarioId, Guid actorTenantId, CreateReunionRequestDto request, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto?> ActualizarAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, UpdateReunionRequestDto request, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto?> IniciarAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto?> ActualizarAsistenciaAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, UpdateReunionAsistenciaRequestDto request, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto?> ActualizarAcuerdosAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, UpdateReunionAcuerdosRequestDto request, CancellationToken cancellationToken = default);

    Task<ReunionDetailDto?> FinalizarAsync(Guid actorUsuarioId, Guid actorTenantId, Guid reunionId, FinalizeReunionRequestDto request, CancellationToken cancellationToken = default);
}