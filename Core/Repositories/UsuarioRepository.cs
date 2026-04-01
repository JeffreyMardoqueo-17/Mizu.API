using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

public sealed class UsuarioRepository : RepositoryBase, IUsuarioRepository
{
    public UsuarioRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<Usuario> CrearUsuarioAsync(Usuario usuario, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO usuarios (id, tenant_id, nombre, apellido, dui, correo, telefono, direccion, password_hash, rol, fecha_creacion)
                           VALUES (@Id, @TenantId, @Nombre, @Apellido, @DUI, @Correo, @Telefono, @Direccion, @PasswordHash, @Rol, @FechaCreacion)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, usuario, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return usuario;
            });
    }

    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, tenant_id, nombre, apellido, dui, correo, telefono, direccion, password_hash, rol, fecha_creacion
                           FROM usuarios
                           WHERE LOWER(correo) = LOWER(@correo)
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { correo }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Usuario>(command);
            });
    }

    public Task<Usuario?> ObtenerPorIdAsync(Guid id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT id, tenant_id, nombre, apellido, dui, correo, telefono, direccion, password_hash, rol, fecha_creacion
                           FROM usuarios
                           WHERE id = @id
                           """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { id }, transaction, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<Usuario>(command);
            });
    }
}
