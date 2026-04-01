using System.Data;
using Microsoft.Extensions.Configuration;
using Muzu.Api.Core.Interfaces;
using Npgsql;

namespace Muzu.Api.Core.Repositories;

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            Environment.GetEnvironmentVariable("MUZU_DB_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontro la conexion de base de datos.");
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
