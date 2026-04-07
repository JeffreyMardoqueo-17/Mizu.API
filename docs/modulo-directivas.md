# Modulo Directivas

## Objetivo

Extender la funcionalidad existente para gestionar periodos administrativos de forma transaccional.

Controller:

- `BoardsController`

Rutas base:

- `/api/boards`

## Endpoints

- `GET /api/boards`
- `GET /api/boards/{id}`
- `POST /api/boards`
- `POST /api/boards/{id}/members`
- `POST /api/boards/{id}/activate`
- `GET /api/boards/{id}/history`

## Reglas clave

- Solo una directiva activa por tenant.
- El cambio de roles hacia cargos se hace solo en directiva.
- Al activar una directiva nueva, la anterior se desactiva y sus miembros regresan a `Socio`.
- Se invalidan sesiones (refresh tokens) de miembros afectados.
- Al asignar un socio a una directiva, se genera contrasena temporal, se marca `must_change_password = true` y se envia correo con credenciales.
- Si una directiva supera su `fecha_fin`, queda vencida como alerta operativa; se mantiene hasta que se active una nueva.

## Constraint SQL aplicado

Se agrego indice parcial unico:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS ux_directiva_tenant_activa
ON directiva(tenant_id)
WHERE estado = 'Activa';
```

## Flujo de activacion atomica

1. Validar directiva objetivo.
2. Desactivar directiva activa anterior.
3. Revertir miembros anteriores a `Socio`.
4. Revocar refresh tokens de miembros anteriores.
5. Asignar cargos de nueva directiva.
6. Activar directiva nueva y registrar historial.

## Flujo de asignacion de miembros

1. Crear directiva en estado `Borrador`.
2. Ingresar al detalle de la directiva y agregar socios con su cargo.
3. Al agregar miembro:
	- se registra en `directiva_miembros`.
	- se genera contrasena temporal.
	- se marca `must_change_password = true`.
	- se revocan sesiones existentes del usuario.
	- se envia correo al socio con usuario (correo) y contrasena temporal.
4. El socio inicia sesion, cambia contrasena en primer acceso y continua normal.
5. Al activar la directiva se garantiza una unica activa por tenant.

## Ejemplo rapido

```http
POST /api/boards/{id}/activate
Authorization: Bearer <token-cookie>
```
