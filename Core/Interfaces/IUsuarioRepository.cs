using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario> CrearUsuarioAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdYTenantAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task AsignarRolAsync(Guid usuarioId, Guid rolId, string rolNombre, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> ExisteOtroUsuarioConRolAsync(Guid tenantId, string rolNombre, Guid excludingUsuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ObtenerPermisosActivosAsync(Guid usuarioId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Usuario> Items, int Total)> ListarPorTenantAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Usuario> Items, int Total)> ListarAvanzadoPorTenantAsync(
        Guid tenantId,
        string? dui,
        string? nombre,
        string? correo,
        bool? estado,
        int page,
        int pageSize,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdYTenantIncluyendoInactivosAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ActualizarDatosAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStateAsync(Guid id, Guid tenantId, bool activo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> EliminarLogicoAsync(Guid id, Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Usuario>> ListarActivosAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Usuario>> ListarSociosActivosAsync(Guid tenantId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> EstablecerPasswordTemporalAsync(Guid id, Guid tenantId, string passwordHash, bool markAsViewed, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> EstablecerPasswordDePeriodoAsync(Guid id, Guid tenantId, string passwordHash, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> CambiarPasswordDefinitivaAsync(Guid id, Guid tenantId, string passwordHash, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
