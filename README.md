# Muzu API - Sistema de Gestión de Agua

API REST para gestión de proyectos de agua (tenants), usuarios y configuraciones.

## Requisitos Previos

- Docker y Docker Compose
- .NET 8.0 (para desarrollo local)

## Configuración

### 1. Variables de Entorno

Crea un archivo `.env` en la raíz del proyecto:

```env
MUZU_DB_PASSWORD=muzu_secure_2024
MUZU_JWT_SECRET=MuzuApiSecretKey12345678901234567890
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_SENDER=your_email@gmail.com
EMAIL_PASSWORD=your_app_password
MUZU_FRONTEND_URL=http://localhost:3000
```

> **Nota**: La clave JWT debe tener al menos 32 caracteres.

### 2. Ejecutar con Docker

```bash
docker compose up -d
```

Esto levantará:
- **PostgreSQL** en `localhost:5432`
- **API** en `http://localhost:5000`

### 3. Ejecutar sin Docker

Establece las variables de entorno y ejecuta:

```bash
export MUZU_DB_CONNECTION="Host=localhost;Database=muzu;Username=muzu_user;Password=muzu_secure_2024"
export MUZU_JWT_SECRET="MuzuApiSecretKey12345678901234567890"
export MUZU_JWT_ISSUER=MuzuApi
export MUZU_JWT_AUDIENCE=MuzuApi

dotnet run
```

---

## Endpoints

### Autenticación

#### Registrar Primer Tenant + Admin
```http
POST /api/auth/register-tenant
Content-Type: application/json

{
  "tenant": {
    "nombre": "Mi Proyecto de Agua",
    "direccion": "San Salvador, El Salvador"
  },
  "usuario": {
    "nombre": "Admin",
    "apellido": "Usuario",
    "dui": "01234567-8",
    "correo": "admin@ejemplo.com",
    "telefono": "7500-0000",
    "direccion": "San Salvador",
    "password": "MiPassword123"
  }
}
```

**Respuesta:**
```json
{
  "usuario": {
    "id": "uuid",
    "tenantId": "uuid",
    "nombre": "Admin",
    "apellido": "Usuario",
    "correo": "admin@ejemplo.com",
    "rol": "Administrador"
  },
  "tenant": {
    "id": "uuid",
    "nombre": "Mi Proyecto de Agua",
    "direccion": "San Salvador, El Salvador"
  }
}
```

**Nota:** Este endpoint solo debe usarse una vez para crear el primer tenant. Intentar usarlo nuevamente resultará en error por restricción de correo único.

---

#### Iniciar Sesión
```http
POST /api/auth/login
Content-Type: application/json

{
  "correo": "admin@ejemplo.com",
  "password": "MiPassword123"
}
```

**Respuesta:**
```json
{
  "tenantId": "uuid-del-tenant",
  "usuarioId": "uuid-del-usuario",
  "rol": "Administrador",
  "refreshToken": "token-para-renovar-sesion"
}
```

**Nota:** El token JWT y refresh token se establecen como cookies HttpOnly automáticamente. El refresh token tiene validez de 24 horas.

---

#### Renovar Sesión (Refresh Token)
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "token-recibido-en-login"
}
```

**Respuesta:**
```json
{
  "tenantId": "uuid-del-tenant",
  "usuarioId": "uuid-del-usuario",
  "rol": "Administrador"
}
```

**Nota:** Este endpoint renueva el access token y genera un nuevo refresh token. El token anterior se invalida automáticamente.

---

### Configuración

#### Obtener Configuración del Tenant
```http
GET /api/config/{tenantId}
```

**Respuesta:**
```json
{
  "id": "uuid",
  "tenantId": "uuid",
  "moneda": "USD",
  "limiteConsumoFijo": 35.00,
  "precioConsumoFijo": 3.00,
  "limiteConsumoExtra1": 45.00,
  "porcentajeExtra1": 0.15,
  "limiteConsumoExtra2": 55.00,
  "porcentajeExtra2": 0.25,
  "multaRetraso": 2.00,
  "multaNoAsistirReunion": 5.00,
  "multaNoAsistirTrabajo": 10.00
}
```

---

## Estructura del Proyecto

```
Muzu.Api/
├── Core/
│   ├── Models/           # Entidades (Tenant, Usuario, etc.)
│   ├── DTOs/             # Data Transfer Objects
│   ├── Interfaces/       # Interfaces de repositorios
│   ├── Repositories/    # Implementaciones (Dapper)
│   ├── Services/         # Lógica de negocio (Auth, JWT)
│   └── Rules/           # Utilidades (PasswordHasher)
├── Controllers/          # Controladores API
├── docker-compose.yml    # Docker Compose
├── init.sql             # Esquema de PostgreSQL
└── Program.cs           # Punto de entrada
```

---

## Esquema de Base de Datos

### Tablas

- **tenants**: Proyectos de agua
- **usuarios**: Usuarios/clientes asociados a tenants
- **tenant_configs**: Configuración de cada tenant (moneda, tarifas, multas)
- **multas**: Catálogo de multas configurables por tenant
- **refresh_tokens**: Tokens de renovación de sesión (24 horas de validez)

### Índices

- `idx_usuarios_correo` - Búsqueda por correo
- `idx_usuarios_tenant` - Filtrado por tenant
- `idx_tenant_configs_tenant` - Configuración por tenant
- `idx_multas_tenant` - Multas por tenant

---

## Comandos Útiles

### Ver logs de la API
```bash
docker logs muzu-api
```

### Ver logs de PostgreSQL
```bash
docker logs muzu-db
```

### Reiniciar servicios
```bash
docker compose restart
```

### Detener servicios
```bash
docker compose down
```

### Eliminar volumen de datos (reset completo)
```bash
docker compose down -v
```

---

## Tecnologías

- **.NET 8.0** - Framework
- **PostgreSQL** - Base de datos
- **Dapper** - ORM ligero
- **BCrypt** - Hash de contraseñas
- **JWT** - Autenticación
- **Docker** - Contenedores
