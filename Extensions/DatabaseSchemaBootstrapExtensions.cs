using Dapper;
using Muzu.Api.Core.Interfaces;

namespace Muzu.Api.Extensions;

public static class DatabaseSchemaBootstrapExtensions
{
    public static async Task EnsureDatabaseSchemaAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        const string sql = """
            ALTER TABLE IF EXISTS tenant_configs
                ADD COLUMN IF NOT EXISTS cargo_extra1 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
                ADD COLUMN IF NOT EXISTS cargo_extra2 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
                ADD COLUMN IF NOT EXISTS limite_consumo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 65,
                ADD COLUMN IF NOT EXISTS cargo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
                ADD COLUMN IF NOT EXISTS cargo_exceso_mayor DECIMAL(10,2) NOT NULL DEFAULT 1.00,
                ADD COLUMN IF NOT EXISTS tramos_consumo_json JSONB NOT NULL DEFAULT '[]'::jsonb;
            """;

        await connection.ExecuteAsync(sql);
    }
}
