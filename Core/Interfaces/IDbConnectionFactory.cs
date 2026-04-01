using System.Data;

namespace Muzu.Api.Core.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();

    IDbConnection? CreateFallbackConnection();
}
