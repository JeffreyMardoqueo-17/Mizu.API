using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Muzu.Api.Core.Interfaces;
using Muzu.Api.Core.Models;

namespace Muzu.Api.Core.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        public async Task<Usuario> CrearUsuarioAsync(Usuario usuario)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            var sql = @"INSERT INTO usuarios (id, tenant_id, nombre, apellido, dui, correo, telefono, direccion, password_hash, rol, fecha_creacion)
                        VALUES (@Id, @TenantId, @Nombre, @Apellido, @DUI, @Correo, @Telefono, @Direccion, @PasswordHash, @Rol, @FechaCreacion)";
            await conn.ExecuteAsync(sql, usuario);
            return usuario;
        }

        public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
        {
            using var conn = DbConnectionFactory.Instance.CreateConnection();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            var sql = "SELECT id, tenant_id, nombre, apellido, dui, correo, telefono, direccion, password_hash, rol, fecha_creacion FROM usuarios WHERE correo = @correo";
            return await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { correo });
        }
    }
}
