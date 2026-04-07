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
            CREATE EXTENSION IF NOT EXISTS pgcrypto;

            CREATE TABLE IF NOT EXISTS roles (
                id UUID PRIMARY KEY,
                nombre VARCHAR(100) NOT NULL UNIQUE,
                descripcion TEXT,
                es_sistema BOOLEAN NOT NULL DEFAULT TRUE,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS permisos (
                id UUID PRIMARY KEY,
                codigo VARCHAR(100) NOT NULL UNIQUE,
                descripcion TEXT,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS rol_permisos (
                rol_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
                permiso_id UUID NOT NULL REFERENCES permisos(id) ON DELETE CASCADE,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                PRIMARY KEY (rol_id, permiso_id)
            );

            CREATE TABLE IF NOT EXISTS usuario_roles (
                id UUID PRIMARY KEY,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                rol_id UUID NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
                fecha_inicio DATE NOT NULL DEFAULT CURRENT_DATE,
                fecha_fin DATE,
                activo BOOLEAN NOT NULL DEFAULT TRUE,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_roles_activo
                ON usuario_roles(usuario_id)
                WHERE activo = TRUE;

            ALTER TABLE IF EXISTS usuarios
                ADD COLUMN IF NOT EXISTS activo BOOLEAN NOT NULL DEFAULT TRUE,
                ADD COLUMN IF NOT EXISTS eliminado BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS fecha_actualizacion TIMESTAMP,
                ADD COLUMN IF NOT EXISTS fecha_eliminacion TIMESTAMP,
                ADD COLUMN IF NOT EXISTS temp_password_generated_at TIMESTAMP,
                ADD COLUMN IF NOT EXISTS temp_password_viewed_at TIMESTAMP;

            CREATE TABLE IF NOT EXISTS docs (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                tipo_documento VARCHAR(50) NOT NULL DEFAULT 'DUI',
                numero_documento VARCHAR(30),
                archivo_url TEXT,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS directiva (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                nombre VARCHAR(120) NOT NULL DEFAULT 'Directiva',
                slug VARCHAR(150) NOT NULL DEFAULT 'directiva',
                fecha_inicio DATE NOT NULL DEFAULT CURRENT_DATE,
                fecha_fin DATE NOT NULL DEFAULT (CURRENT_DATE + INTERVAL '1 year')::date,
                estado VARCHAR(20) NOT NULL DEFAULT 'Borrador',
                administrador_responsable_id UUID,
                periodo_inicio DATE NOT NULL,
                periodo_fin DATE NOT NULL,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                CONSTRAINT ck_directiva_periodo CHECK (periodo_fin > periodo_inicio)
            );

            ALTER TABLE IF EXISTS directiva
                ADD COLUMN IF NOT EXISTS nombre VARCHAR(120) NOT NULL DEFAULT 'Directiva',
                ADD COLUMN IF NOT EXISTS slug VARCHAR(150) NOT NULL DEFAULT 'directiva',
                ADD COLUMN IF NOT EXISTS fecha_inicio DATE,
                ADD COLUMN IF NOT EXISTS fecha_fin DATE,
                ADD COLUMN IF NOT EXISTS estado VARCHAR(20) NOT NULL DEFAULT 'Borrador',
                ADD COLUMN IF NOT EXISTS administrador_responsable_id UUID,
                ADD COLUMN IF NOT EXISTS fecha_actualizacion TIMESTAMP,
                ADD COLUMN IF NOT EXISTS fecha_transicion TIMESTAMP;

            UPDATE directiva
            SET fecha_inicio = COALESCE(fecha_inicio, periodo_inicio),
                fecha_fin = COALESCE(fecha_fin, periodo_fin)
            WHERE fecha_inicio IS NULL OR fecha_fin IS NULL;

            CREATE TABLE IF NOT EXISTS directiva_miembros (
                id UUID PRIMARY KEY,
                directiva_id UUID NOT NULL REFERENCES directiva(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
                rol_id UUID NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
                cargo VARCHAR(50),
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                UNIQUE (directiva_id, usuario_id)
            );

            ALTER TABLE IF EXISTS directiva_miembros
                ADD COLUMN IF NOT EXISTS cargo VARCHAR(50);

            CREATE TABLE IF NOT EXISTS directiva_historial (
                id UUID PRIMARY KEY,
                board_id UUID NOT NULL REFERENCES directiva(id) ON DELETE CASCADE,
                evento VARCHAR(100) NOT NULL,
                descripcion TEXT NOT NULL,
                actor_usuario_id UUID,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS partner_documents (
                id UUID PRIMARY KEY,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                document_url TEXT NOT NULL,
                cloudinary_public_id VARCHAR(500) NOT NULL,
                document_type VARCHAR(50) NOT NULL DEFAULT 'DUI',
                file_name VARCHAR(500) NOT NULL,
                file_size_bytes BIGINT NOT NULL,
                display_order SMALLINT NOT NULL CHECK (display_order >= 1 AND display_order <= 2),
                activo BOOLEAN NOT NULL DEFAULT TRUE,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                fecha_actualizacion TIMESTAMP,
                fecha_eliminacion TIMESTAMP,
                eliminado BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE INDEX IF NOT EXISTS idx_docs_tenant ON docs(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_docs_usuario ON docs(usuario_id);
            CREATE INDEX IF NOT EXISTS idx_directiva_tenant ON directiva(tenant_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_directiva_tenant_activa ON directiva(tenant_id) WHERE estado = 'Activa';
            CREATE INDEX IF NOT EXISTS idx_directiva_miembros_directiva ON directiva_miembros(directiva_id);
            CREATE INDEX IF NOT EXISTS idx_directiva_miembros_usuario ON directiva_miembros(usuario_id);
            CREATE INDEX IF NOT EXISTS idx_directiva_historial_board ON directiva_historial(board_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_partner_documents_usuario_display_order_activo
                ON partner_documents(usuario_id, display_order)
                WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_partner_documents_usuario ON partner_documents(usuario_id) WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_partner_documents_tenant ON partner_documents(tenant_id) WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_partner_documents_cloudinary_public_id ON partner_documents(cloudinary_public_id);
            CREATE INDEX IF NOT EXISTS idx_partner_documents_fecha_creacion ON partner_documents(fecha_creacion DESC);

            INSERT INTO roles (id, nombre, descripcion, es_sistema)
            VALUES
                ('00000000-0000-0000-0000-000000000001', 'Administrador', 'Control total del sistema.', TRUE),
                ('00000000-0000-0000-0000-000000000002', 'Presidente', 'Gestion operativa y supervision general.', TRUE),
                ('00000000-0000-0000-0000-000000000003', 'Tesorero', 'Gestion de cobros, ingresos y reportes de caja.', TRUE),
                ('00000000-0000-0000-0000-000000000004', 'Contador', 'Revision contable y conciliacion.', TRUE),
                ('00000000-0000-0000-0000-000000000005', 'Socio', 'Acceso de solo lectura a su informacion.', TRUE),
                ('00000000-0000-0000-0000-000000000006', 'Secretario', 'Gestion documental de directiva.', TRUE),
                ('00000000-0000-0000-0000-000000000007', 'Vocal', 'Participacion operativa en directiva.', TRUE)
            ON CONFLICT (nombre) DO NOTHING;

            INSERT INTO permisos (id, codigo, descripcion)
            VALUES
                ('00000000-0000-0000-0000-100000000001', 'usuarios.read', 'Ver usuarios.'),
                ('00000000-0000-0000-0000-100000000002', 'usuarios.create', 'Crear usuarios.'),
                ('00000000-0000-0000-0000-100000000003', 'usuarios.update', 'Actualizar usuarios.'),
                ('00000000-0000-0000-0000-100000000004', 'usuarios.assign-role', 'Cambiar roles de usuarios.'),
                ('00000000-0000-0000-0000-100000000005', 'config.read', 'Ver configuracion del tenant.'),
                ('00000000-0000-0000-0000-100000000006', 'config.update', 'Actualizar configuracion del tenant.'),
                ('00000000-0000-0000-0000-100000000007', 'pagos.read', 'Consultar historial de pagos.'),
                ('00000000-0000-0000-0000-100000000008', 'multas.read', 'Consultar historial de multas.'),
                ('00000000-0000-0000-0000-100000000009', 'directiva.manage', 'Gestionar periodos de directiva.'),
                ('00000000-0000-0000-0000-100000000010', 'docs.manage', 'Gestionar documentos de socios.')
            ON CONFLICT (codigo) DO NOTHING;

            INSERT INTO rol_permisos (rol_id, permiso_id)
            SELECT r.id, p.id
            FROM roles r
            CROSS JOIN permisos p
            WHERE r.nombre = 'Administrador'
            ON CONFLICT (rol_id, permiso_id) DO NOTHING;

            INSERT INTO rol_permisos (rol_id, permiso_id)
            SELECT r.id, p.id
            FROM roles r
            INNER JOIN permisos p ON p.codigo IN ('usuarios.read', 'usuarios.create', 'usuarios.update', 'usuarios.assign-role', 'config.read', 'config.update', 'pagos.read', 'multas.read', 'directiva.manage', 'docs.manage')
            WHERE r.nombre = 'Presidente'
            ON CONFLICT (rol_id, permiso_id) DO NOTHING;

            INSERT INTO rol_permisos (rol_id, permiso_id)
            SELECT r.id, p.id
            FROM roles r
            INNER JOIN permisos p ON p.codigo IN ('usuarios.read', 'config.read', 'pagos.read', 'multas.read', 'docs.manage')
            WHERE r.nombre = 'Tesorero'
            ON CONFLICT (rol_id, permiso_id) DO NOTHING;

            INSERT INTO rol_permisos (rol_id, permiso_id)
            SELECT r.id, p.id
            FROM roles r
            INNER JOIN permisos p ON p.codigo IN ('usuarios.read', 'pagos.read', 'multas.read')
            WHERE r.nombre = 'Contador'
            ON CONFLICT (rol_id, permiso_id) DO NOTHING;

            INSERT INTO rol_permisos (rol_id, permiso_id)
            SELECT r.id, p.id
            FROM roles r
            INNER JOIN permisos p ON p.codigo IN ('pagos.read', 'multas.read')
            WHERE r.nombre = 'Socio'
            ON CONFLICT (rol_id, permiso_id) DO NOTHING;

            INSERT INTO roles (id, nombre, descripcion, es_sistema)
            SELECT gen_random_uuid(), INITCAP(TRIM(u.rol)), 'Rol importado desde usuarios.rol', FALSE
            FROM usuarios u
            WHERE u.rol IS NOT NULL
              AND TRIM(u.rol) <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM roles r
                  WHERE LOWER(r.nombre) = LOWER(TRIM(u.rol))
              )
            GROUP BY INITCAP(TRIM(u.rol));

            INSERT INTO usuario_roles (id, usuario_id, rol_id, fecha_inicio, activo, fecha_creacion)
            SELECT gen_random_uuid(), u.id, r.id, CURRENT_DATE, TRUE, NOW()
            FROM usuarios u
            INNER JOIN roles r ON LOWER(r.nombre) = LOWER(COALESCE(NULLIF(TRIM(u.rol), ''), 'Socio'))
            WHERE NOT EXISTS (
                SELECT 1
                FROM usuario_roles ur
                WHERE ur.usuario_id = u.id
                  AND ur.activo = TRUE
            );

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
