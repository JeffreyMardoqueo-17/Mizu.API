-- Muzu Database Schema

-- Tenants table
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY,
    nombre VARCHAR(255) NOT NULL,
    direccion TEXT NOT NULL,
    logo_url TEXT,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Users table
CREATE TABLE IF NOT EXISTS usuarios (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    nombre VARCHAR(255) NOT NULL,
    apellido VARCHAR(255) NOT NULL,
    dui VARCHAR(20) NOT NULL,
    correo VARCHAR(255) NOT NULL UNIQUE,
    telefono VARCHAR(20) NOT NULL,
    direccion TEXT NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    rol VARCHAR(50) NOT NULL DEFAULT 'Usuario',
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Tenant configuration table
CREATE TABLE IF NOT EXISTS tenant_configs (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id) UNIQUE,
    moneda VARCHAR(10) NOT NULL DEFAULT 'USD',
    limite_consumo_fijo DECIMAL(10,2) NOT NULL DEFAULT 35,
    precio_consumo_fijo DECIMAL(10,2) NOT NULL DEFAULT 3,
    limite_consumo_extra1 DECIMAL(10,2) NOT NULL DEFAULT 45,
    cargo_extra1 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
    limite_consumo_extra2 DECIMAL(10,2) NOT NULL DEFAULT 55,
    cargo_extra2 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
    limite_consumo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 65,
    cargo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
    cargo_exceso_mayor DECIMAL(10,2) NOT NULL DEFAULT 1.00,
    tramos_consumo_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    multa_retraso DECIMAL(10,2) NOT NULL DEFAULT 2,
    multa_no_asistir_reunion DECIMAL(10,2) NOT NULL DEFAULT 5,
    multa_no_asistir_trabajo DECIMAL(10,2) NOT NULL DEFAULT 10
);

ALTER TABLE tenant_configs
    ADD COLUMN IF NOT EXISTS cargo_extra1 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
    ADD COLUMN IF NOT EXISTS cargo_extra2 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
    ADD COLUMN IF NOT EXISTS limite_consumo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 65,
    ADD COLUMN IF NOT EXISTS cargo_extra3 DECIMAL(10,2) NOT NULL DEFAULT 0.50,
    ADD COLUMN IF NOT EXISTS cargo_exceso_mayor DECIMAL(10,2) NOT NULL DEFAULT 1.00,
    ADD COLUMN IF NOT EXISTS tramos_consumo_json JSONB NOT NULL DEFAULT '[]'::jsonb;

-- Fines/Rules table
CREATE TABLE IF NOT EXISTS multas (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    nombre VARCHAR(255) NOT NULL,
    monto DECIMAL(10,2) NOT NULL,
    descripcion TEXT
);

-- Refresh tokens table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY,
    usuario_id UUID NOT NULL REFERENCES usuarios(id),
    token VARCHAR(255) NOT NULL UNIQUE,
    expira TIMESTAMP NOT NULL,
    revokeado BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_usuarios_correo ON usuarios(correo);
CREATE INDEX IF NOT EXISTS idx_usuarios_tenant ON usuarios(tenant_id);
CREATE INDEX IF NOT EXISTS idx_tenant_configs_tenant ON tenant_configs(tenant_id);
CREATE INDEX IF NOT EXISTS idx_multas_tenant ON multas(tenant_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_usuario ON refresh_tokens(usuario_id);
