using System.Data;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories;

/// <summary>
/// Repositorio para operaciones de datos de documentos de partners.
/// Maneja todas las operaciones CRUD con la base de datos PostgreSQL.
/// </summary>
public sealed class PartnerDocumentRepository : RepositoryBase, IPartnerDocumentRepository
{
    private const string SelectPartnerDocumentSql = """
        SELECT 
            id,
            usuario_id as "UsuarioId",
            tenant_id as "TenantId",
            document_url as "DocumentUrl",
            cloudinary_public_id as "CloudinaryPublicId",
            document_type as "DocumentType",
            file_name as "FileName",
            file_size_bytes as "FileSizeBytes",
            display_order as "DisplayOrder",
            activo,
            fecha_creacion as "FechaCreacion",
            fecha_actualizacion as "FechaActualizacion",
            fecha_eliminacion as "FechaEliminacion",
            eliminado
        FROM partner_documents
        WHERE eliminado = FALSE
        """;

    public PartnerDocumentRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<PartnerDocument>> ObtenerPorUsuarioIdAsync(
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var sql = SelectPartnerDocumentSql + " AND usuario_id = @UsuarioId ORDER BY display_order ASC, fecha_creacion DESC";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                var result = await connection.QueryAsync<PartnerDocument>(command);
                return (IReadOnlyList<PartnerDocument>)result.ToList();
            });
    }

    public Task<PartnerDocument?> ObtenerPorIdAsync(
        Guid id,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var sql = SelectPartnerDocumentSql + " AND id = @Id";

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { Id = id },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.QueryFirstOrDefaultAsync<PartnerDocument>(command);
            });
    }

    public Task<int> ContarDocumentosActivosAsync(
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM partner_documents
            WHERE usuario_id = @UsuarioId 
            AND activo = TRUE 
            AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<int>(command);
            });
    }

    public Task<PartnerDocument> CrearAsync(
        PartnerDocument documento,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO partner_documents (
                id, usuario_id, tenant_id, document_url, cloudinary_public_id,
                document_type, file_name, file_size_bytes, display_order,
                activo, fecha_creacion, fecha_actualizacion, fecha_eliminacion, eliminado
            )
            VALUES (
                @Id, @UsuarioId, @TenantId, @DocumentUrl, @CloudinaryPublicId,
                @DocumentType, @FileName, @FileSizeBytes, @DisplayOrder,
                @Activo, @FechaCreacion, @FechaActualizacion, @FechaEliminacion, @Eliminado
            )
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, documento, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
                return documento;
            });
    }

    public Task<bool> ActualizarAsync(
        PartnerDocument documento,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE partner_documents
            SET 
                document_url = @DocumentUrl,
                cloudinary_public_id = @CloudinaryPublicId,
                document_type = @DocumentType,
                file_name = @FileName,
                file_size_bytes = @FileSizeBytes,
                display_order = @DisplayOrder,
                activo = @Activo,
                fecha_actualizacion = @FechaActualizacion
            WHERE id = @Id AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, documento, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }

    public Task<bool> EliminarAsync(
        Guid id,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE partner_documents
            SET eliminado = TRUE, activo = FALSE, fecha_eliminacion = CURRENT_TIMESTAMP
            WHERE id = @Id
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken);
                var affectedRows = await connection.ExecuteAsync(command);
                return affectedRows > 0;
            });
    }

    public Task<bool> ExisteYPerteneceAlUsuarioAsync(
        Guid documentoId,
        Guid usuarioId,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*) > 0
            FROM partner_documents
            WHERE id = @DocumentoId AND usuario_id = @UsuarioId AND eliminado = FALSE
            """;

        return WithConnectionAsync(
            transaction,
            async connection =>
            {
                var command = new CommandDefinition(
                    sql,
                    new { DocumentoId = documentoId, UsuarioId = usuarioId },
                    transaction,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<bool>(command);
            });
    }
}
