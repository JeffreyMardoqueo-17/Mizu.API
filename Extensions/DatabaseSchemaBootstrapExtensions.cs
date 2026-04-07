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
                ADD COLUMN IF NOT EXISTS niu BIGINT,
                ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS fecha_actualizacion TIMESTAMP,
                ADD COLUMN IF NOT EXISTS fecha_eliminacion TIMESTAMP,
                ADD COLUMN IF NOT EXISTS temp_password_generated_at TIMESTAMP,
                ADD COLUMN IF NOT EXISTS temp_password_viewed_at TIMESTAMP;

            WITH tenant_base AS (
                SELECT
                    u.tenant_id,
                    COALESCE(MAX(u.niu), 0) AS base_niu
                FROM usuarios u
                GROUP BY u.tenant_id
            ),
            ranked_usuarios AS (
                SELECT
                    u.id,
                    u.tenant_id,
                    tenant_base.base_niu + ROW_NUMBER() OVER (
                        PARTITION BY u.tenant_id
                        ORDER BY u.fecha_creacion, u.id
                    ) AS niu_calculado
                FROM usuarios u
                INNER JOIN tenant_base ON tenant_base.tenant_id = u.tenant_id
                WHERE u.niu IS NULL
            )
            UPDATE usuarios u
            SET niu = ranked_usuarios.niu_calculado
            FROM ranked_usuarios
            WHERE u.id = ranked_usuarios.id
              AND u.tenant_id = ranked_usuarios.tenant_id;

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

            CREATE TABLE IF NOT EXISTS reuniones (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                titulo VARCHAR(180) NOT NULL,
                fecha_reunion DATE NOT NULL,
                hora_inicio TIME NOT NULL,
                hora_fin TIME,
                estado VARCHAR(20) NOT NULL DEFAULT 'Programada',
                puntos_tratar_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                acuerdos_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                notas_secretaria TEXT,
                creado_por_usuario_id UUID REFERENCES usuarios(id) ON DELETE SET NULL,
                iniciada_at TIMESTAMP,
                finalizada_at TIMESTAMP,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                fecha_actualizacion TIMESTAMP,
                CONSTRAINT ck_reuniones_estado CHECK (estado IN ('Programada', 'EnCurso', 'Finalizada', 'Cancelada'))
            );

            CREATE TABLE IF NOT EXISTS reunion_asistencias (
                id UUID PRIMARY KEY,
                reunion_id UUID NOT NULL REFERENCES reuniones(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                asistio BOOLEAN NOT NULL DEFAULT FALSE,
                observacion TEXT,
                marcado_por_usuario_id UUID REFERENCES usuarios(id) ON DELETE SET NULL,
                fecha_marcado TIMESTAMP,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                fecha_actualizacion TIMESTAMP,
                UNIQUE (reunion_id, usuario_id)
            );

            CREATE TABLE IF NOT EXISTS reunion_historial (
                id UUID PRIMARY KEY,
                reunion_id UUID NOT NULL REFERENCES reuniones(id) ON DELETE CASCADE,
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

            CREATE TABLE IF NOT EXISTS medidores (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                numero_medidor BIGINT NOT NULL,
                activo BOOLEAN NOT NULL DEFAULT TRUE,
                fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW(),
                fecha_actualizacion TIMESTAMP,
                eliminado BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE INDEX IF NOT EXISTS idx_docs_tenant ON docs(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_docs_usuario ON docs(usuario_id);
            CREATE INDEX IF NOT EXISTS idx_directiva_tenant ON directiva(tenant_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_directiva_tenant_activa ON directiva(tenant_id) WHERE estado = 'Activa';
            CREATE INDEX IF NOT EXISTS idx_directiva_miembros_directiva ON directiva_miembros(directiva_id);
            CREATE INDEX IF NOT EXISTS idx_directiva_miembros_usuario ON directiva_miembros(usuario_id);
            CREATE INDEX IF NOT EXISTS idx_directiva_historial_board ON directiva_historial(board_id);
            CREATE INDEX IF NOT EXISTS idx_reuniones_tenant ON reuniones(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_reuniones_fecha ON reuniones(tenant_id, fecha_reunion DESC);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_reunion_asistencias_reunion_usuario ON reunion_asistencias(reunion_id, usuario_id);
            CREATE INDEX IF NOT EXISTS idx_reunion_asistencias_reunion ON reunion_asistencias(reunion_id);
            CREATE INDEX IF NOT EXISTS idx_reunion_historial_reunion ON reunion_historial(reunion_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_partner_documents_usuario_display_order_activo
                ON partner_documents(usuario_id, display_order)
                WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_partner_documents_usuario ON partner_documents(usuario_id) WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_partner_documents_tenant ON partner_documents(tenant_id) WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_partner_documents_cloudinary_public_id ON partner_documents(cloudinary_public_id);
            CREATE INDEX IF NOT EXISTS idx_partner_documents_fecha_creacion ON partner_documents(fecha_creacion DESC);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_usuarios_tenant_niu ON usuarios(tenant_id, niu) WHERE eliminado = FALSE;
            CREATE UNIQUE INDEX IF NOT EXISTS ux_medidores_tenant_numero ON medidores(tenant_id, numero_medidor) WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_medidores_usuario ON medidores(usuario_id) WHERE eliminado = FALSE;
            CREATE INDEX IF NOT EXISTS idx_medidores_tenant ON medidores(tenant_id) WHERE eliminado = FALSE;

            CREATE TABLE IF NOT EXISTS billing_cycles (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                period_code VARCHAR(20) NOT NULL,
                period_start DATE NOT NULL,
                period_end DATE NOT NULL,
                due_date DATE NOT NULL,
                issue_date DATE NOT NULL,
                frequency VARCHAR(20) NOT NULL DEFAULT 'monthly',
                status VARCHAR(20) NOT NULL DEFAULT 'abierto',
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                closed_at TIMESTAMP,
                UNIQUE (tenant_id, period_code)
            );

            CREATE TABLE IF NOT EXISTS meter_readings (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                meter_id UUID NOT NULL REFERENCES medidores(id) ON DELETE CASCADE,
                billing_cycle_id UUID NOT NULL REFERENCES billing_cycles(id) ON DELETE CASCADE,
                read_at DATE NOT NULL,
                previous_reading DECIMAL(18,3) NOT NULL DEFAULT 0,
                current_reading DECIMAL(18,3) NOT NULL,
                consumption_m3 DECIMAL(18,3) NOT NULL,
                source VARCHAR(30) NOT NULL DEFAULT 'manual',
                notes TEXT,
                created_by UUID REFERENCES usuarios(id),
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP,
                UNIQUE (tenant_id, meter_id, billing_cycle_id),
                CONSTRAINT ck_meter_readings_monotonic CHECK (current_reading >= previous_reading)
            );

            CREATE TABLE IF NOT EXISTS invoices (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                meter_id UUID NOT NULL REFERENCES medidores(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                billing_cycle_id UUID NOT NULL REFERENCES billing_cycles(id) ON DELETE CASCADE,
                meter_reading_id UUID NOT NULL REFERENCES meter_readings(id) ON DELETE CASCADE,
                invoice_number VARCHAR(60) NOT NULL,
                status VARCHAR(30) NOT NULL DEFAULT 'emitido',
                currency VARCHAR(10) NOT NULL DEFAULT 'USD',
                subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                previous_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                late_fee_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                operational_penalty_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                adjustments_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                paid_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                pending_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                issued_at TIMESTAMP,
                due_date DATE NOT NULL,
                paid_at TIMESTAMP,
                cancelled_at TIMESTAMP,
                reliquidated_from_invoice_id UUID REFERENCES invoices(id),
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMP,
                UNIQUE (tenant_id, invoice_number)
            );

            CREATE TABLE IF NOT EXISTS invoice_lines (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
                line_type VARCHAR(40) NOT NULL,
                description TEXT NOT NULL,
                quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
                unit_price DECIMAL(18,2) NOT NULL DEFAULT 0,
                amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                reference_table VARCHAR(80),
                reference_id UUID,
                metadata JSONB,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS payments (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
                meter_id UUID NOT NULL REFERENCES medidores(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                payment_date DATE NOT NULL,
                amount DECIMAL(18,2) NOT NULL,
                method VARCHAR(30) NOT NULL,
                reference VARCHAR(120),
                status VARCHAR(20) NOT NULL DEFAULT 'aprobado',
                notes TEXT,
                created_by UUID REFERENCES usuarios(id),
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS late_fee_history (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                meter_id UUID NOT NULL REFERENCES medidores(id) ON DELETE CASCADE,
                source_invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
                target_invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
                amount DECIMAL(18,2) NOT NULL,
                generated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                rule_snapshot JSONB,
                UNIQUE (tenant_id, source_invoice_id)
            );

            CREATE TABLE IF NOT EXISTS operational_penalties (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                usuario_id UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                source_type VARCHAR(30) NOT NULL,
                source_date DATE NOT NULL,
                amount DECIMAL(18,2) NOT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'pendiente',
                assignment_strategy VARCHAR(40) NOT NULL DEFAULT 'primary_meter',
                assigned_meter_id UUID REFERENCES medidores(id),
                assigned_invoice_id UUID REFERENCES invoices(id),
                assigned_at TIMESTAMP,
                notes TEXT,
                created_by UUID REFERENCES usuarios(id),
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS carried_balances (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                meter_id UUID NOT NULL REFERENCES medidores(id) ON DELETE CASCADE,
                source_invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
                target_invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
                amount DECIMAL(18,2) NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS invoice_adjustments (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                meter_id UUID NOT NULL REFERENCES medidores(id) ON DELETE CASCADE,
                invoice_id UUID REFERENCES invoices(id) ON DELETE SET NULL,
                adjustment_type VARCHAR(30) NOT NULL,
                amount DECIMAL(18,2) NOT NULL,
                reason TEXT NOT NULL,
                source_reading_id UUID REFERENCES meter_readings(id),
                source_invoice_id UUID REFERENCES invoices(id),
                linked_invoice_id UUID REFERENCES invoices(id),
                effective_cycle_id UUID REFERENCES billing_cycles(id),
                status VARCHAR(20) NOT NULL DEFAULT 'aplicado',
                created_by UUID REFERENCES usuarios(id),
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_billing_cycles_tenant_period ON billing_cycles(tenant_id, period_code);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_meter_readings_tenant_meter_cycle ON meter_readings(tenant_id, meter_id, billing_cycle_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_invoices_tenant_number ON invoices(tenant_id, invoice_number);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_invoices_tenant_meter_cycle_active ON invoices(tenant_id, meter_id, billing_cycle_id) WHERE status <> 'anulado';
            CREATE INDEX IF NOT EXISTS idx_invoices_tenant_meter ON invoices(tenant_id, meter_id) WHERE status <> 'anulado';
            CREATE INDEX IF NOT EXISTS idx_invoice_lines_invoice ON invoice_lines(invoice_id);
            CREATE INDEX IF NOT EXISTS idx_payments_invoice ON payments(invoice_id);
            CREATE INDEX IF NOT EXISTS idx_late_fee_history_tenant ON late_fee_history(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_operational_penalties_tenant_status ON operational_penalties(tenant_id, status);
            CREATE INDEX IF NOT EXISTS idx_carried_balances_tenant_meter ON carried_balances(tenant_id, meter_id);
            CREATE INDEX IF NOT EXISTS idx_invoice_adjustments_tenant_meter ON invoice_adjustments(tenant_id, meter_id);

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
                ('00000000-0000-0000-0000-100000000010', 'docs.manage', 'Gestionar documentos de socios.'),
                ('00000000-0000-0000-0000-100000000011', 'reuniones.manage', 'Gestionar reuniones y asistencias.')
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
            INNER JOIN permisos p ON p.codigo IN ('usuarios.read', 'config.read', 'pagos.read', 'multas.read', 'docs.manage', 'reuniones.manage')
            WHERE r.nombre = 'Secretario'
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

            INSERT INTO medidores (
                id,
                tenant_id,
                usuario_id,
                numero_medidor,
                activo,
                fecha_creacion,
                fecha_actualizacion,
                eliminado
            )
            SELECT
                gen_random_uuid(),
                ordered.tenant_id,
                ordered.id,
                ordered.numero_medidor,
                TRUE,
                NOW(),
                NOW(),
                FALSE
            FROM (
                SELECT
                    u.id,
                    u.tenant_id,
                    ROW_NUMBER() OVER (
                        PARTITION BY u.tenant_id
                        ORDER BY u.niu, u.fecha_creacion, u.id
                    ) AS numero_medidor
                FROM usuarios u
                WHERE u.eliminado = FALSE
            ) ordered
            WHERE NOT EXISTS (
                SELECT 1
                FROM medidores m
                WHERE m.usuario_id = ordered.id
                  AND m.tenant_id = ordered.tenant_id
                  AND m.eliminado = FALSE
            );

            ALTER TABLE IF EXISTS tenant_configs
                ADD COLUMN IF NOT EXISTS cargo_extra1 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
                ADD COLUMN IF NOT EXISTS cargo_extra2 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
                ADD COLUMN IF NOT EXISTS limite_consumo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 65,
                ADD COLUMN IF NOT EXISTS cargo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
                ADD COLUMN IF NOT EXISTS cargo_exceso_mayor DECIMAL(10,2) NOT NULL DEFAULT 1.00,
                ADD COLUMN IF NOT EXISTS tramos_consumo_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                ADD COLUMN IF NOT EXISTS permitir_multiples_contadores BOOLEAN NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS maximo_contadores_por_usuario INTEGER NOT NULL DEFAULT 1;

            UPDATE tenant_configs
            SET maximo_contadores_por_usuario = 1
            WHERE maximo_contadores_por_usuario < 1;
            """;

        await connection.ExecuteAsync(sql);
    }
}
