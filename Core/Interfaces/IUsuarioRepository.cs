using System.Data;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario> CrearUsuarioAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
