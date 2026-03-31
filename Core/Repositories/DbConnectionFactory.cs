using System.Data;
using Npgsql;

namespace Muzu.Api.Core.Repositories
{
    public sealed class DbConnectionFactory
    {
        private static readonly Lazy<DbConnectionFactory> _instance = new(() => new DbConnectionFactory());
        private readonly string _connectionString;

        private DbConnectionFactory()
        {
            _connectionString = Environment.GetEnvironmentVariable("MUZU_DB_CONNECTION") ?? throw new Exception("No se encontró la variable de entorno MUZU_DB_CONNECTION");
        }

        public static DbConnectionFactory Instance => _instance.Value;

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
