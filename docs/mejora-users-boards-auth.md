# Mejora Users + Boards + Auth Security

## Alcance

Se extendio la implementacion existente sin rehacer desde cero, manteniendo compatibilidad con rutas y flujo actuales.

## Backend

### Endpoints extendidos/agregados

- `GET /api/users`
- `POST /api/users`
- `PUT /api/users/{id}`
- `GET /api/users/{id}`
- `DELETE /api/users/{id}` (eliminacion logica)
- `GET /api/boards`
- `GET /api/boards/{id}`
- `POST /api/boards`
- `POST /api/boards/{id}/members`
- `POST /api/boards/{id}/activate`
- `GET /api/boards/{id}/history`
- `POST /api/auth/change-temporary-password`
- `POST /api/auth/regenerate-temp-password`
- `POST /api/auth/invalidate-board-sessions`

### Refactors clave

- Se agregaron servicios:
  - `UsersService`
  - `BoardsService`
  - `AuthSecurityService`
- Se agrego repositorio:
  - `BoardRepository`
- Se extendio `UsuarioRepository` con:
  - filtros avanzados (dui, nombre, correo, estado)
  - edicion general
  - eliminacion logica
  - password temporal

### Transacciones criticas

Activacion de directiva (`POST /api/boards/{id}/activate`) en una sola transaccion:

1. desactiva directiva activa anterior
2. devuelve miembros anteriores a `Socio`
3. invalida refresh tokens de miembros anteriores
4. aplica cargos de la nueva directiva
5. activa nueva directiva
6. registra historial

## PostgreSQL

### Migraciones/DDL aplicadas en `init.sql` + bootstrap

- `usuarios`: columnas `activo`, `eliminado`, `must_change_password`, timestamps de auditoria y password temporal.
- `directiva`: columnas `nombre`, `slug`, `fecha_inicio`, `fecha_fin`, `estado`, `administrador_responsable_id`, `fecha_actualizacion`, `fecha_transicion`.
- `directiva_miembros`: columna `cargo`.
- `directiva_historial`: tabla nueva para auditoria de eventos.

### Constraint e indices

- indice parcial unico para una sola directiva activa por tenant:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS ux_directiva_tenant_activa
ON directiva(tenant_id)
WHERE estado = 'Activa';
```

## Seguridad

- JWT por cookie HttpOnly (`muzu_token`) en middleware de autenticacion.
- flujo `mustChangePassword` habilitado en login.

## Compatibilidad backward

- Se mantienen endpoints legacy de `Auth` y `Usuarios`.
- El cambio de rol directo en `/api/usuarios/{id}/rol` queda bloqueado para mover la regla al modulo de directivas.
