using System.Data;
using Muzu.Api.Core.Interfaces;
using Npgsql;

namespace Muzu.Api.Core.Repositories;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly string? _fallbackConnectionString;

    public DbConnectionFactory()
    {
        var rawConnectionString =
            Environment.GetEnvironmentVariable("MUZU_DB_CONNECTION")
            ?? throw new InvalidOperationException("No se encontro MUZU_DB_CONNECTION.");

        var normalized = rawConnectionString.Trim().Trim('"', '\'');
        var builder = new NpgsqlConnectionStringBuilder(normalized);

        _connectionString = builder.ConnectionString;

        var isContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!isContainer && string.Equals(builder.Host, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackBuilder = new NpgsqlConnectionStringBuilder(builder.ConnectionString)
            {
                Host = "localhost"
            };
            _fallbackConnectionString = fallbackBuilder.ConnectionString;
            return;
        }

        if (isContainer &&
            (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(builder.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)))
        {
            var fallbackBuilder = new NpgsqlConnectionStringBuilder(builder.ConnectionString)
            {
                Host = "postgres"
            };
            _fallbackConnectionString = fallbackBuilder.ConnectionString;
        }
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public IDbConnection? CreateFallbackConnection()
    {
        if (string.IsNullOrWhiteSpace(_fallbackConnectionString))
        {
            return null;
        }

        return new NpgsqlConnection(_fallbackConnectionString);
    }
}
