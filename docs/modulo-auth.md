# Modulo Auth

## Objetivo

En este modulo concentre todo lo relacionado con autenticacion y registro inicial del sistema.

Controller asociado:

- `AuthController`

Ruta base:

- `/api/auth`

URL local de prueba:

- `http://localhost:5177`
- `https://localhost:7227`

## Endpoints del modulo

- `POST /api/auth/register-tenant`
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

## 1. Registrar tenant y usuario administrador

Ruta:

```http
POST /api/auth/register-tenant
Content-Type: application/json
```

Body de ejemplo:

```json
{
  "tenant": {
    "nombre": "Residencial Los Robles",
    "direccion": "San Salvador, Calle Principal #12"
  },
  "usuario": {
    "nombre": "Carlos",
    "apellido": "Martinez",
    "dui": "12345678-9",
    "correo": "admin@losrobles.com",
    "telefono": "7000-1111",
    "direccion": "Casa comun, Residencial Los Robles",
    "password": "Admin12345"
  }
}
```

Respuesta esperada `200 OK`:

```json
{
  "usuario": {
    "id": "11111111-1111-1111-1111-111111111111",
    "tenantId": "22222222-2222-2222-2222-222222222222",
    "nombre": "Carlos",
    "apellido": "Martinez",
    "correo": "admin@losrobles.com",
    "telefono": "7000-1111",
    "direccion": "Casa comun, Residencial Los Robles",
    "rol": "Administrador",
    "fechaCreacion": "2026-03-31T18:00:00Z"
  },
  "tenant": {
    "id": "22222222-2222-2222-2222-222222222222",
    "nombre": "Residencial Los Robles",
    "direccion": "San Salvador, Calle Principal #12",
    "logoUrl": null,
    "fechaCreacion": "2026-03-31T18:00:00Z",
    "configuracion": {
      "id": "33333333-3333-3333-3333-333333333333",
      "tenantId": "22222222-2222-2222-2222-222222222222",
      "moneda": "USD",
      "limiteConsumoFijo": 35,
      "precioConsumoFijo": 3,
      "limiteConsumoExtra1": 45,
      "porcentajeExtra1": 0.15,
      "limiteConsumoExtra2": 55,
      "porcentajeExtra2": 0.25,
      "multaRetraso": 2,
      "multaNoAsistirReunion": 5,
      "multaNoAsistirTrabajo": 10
    }
  }
}
```

Errores esperados:

- `409 Conflict` si ya existe un tenant con ese nombre.
- `409 Conflict` si ya existe un usuario con ese correo.
- `400 BadRequest` si falta algun campo requerido o no cumple validacion.

Notas de prueba:

- Al registrarse, el backend tambien escribe las cookies `muzu_token` y `muzu_refresh_token`.
- De esta respuesta debo guardar el `tenant.id` porque me sirve para probar el modulo de configuracion.

## 2. Login

Ruta:

```http
POST /api/auth/login
Content-Type: application/json
```

Body de ejemplo:

```json
{
  "correo": "admin@losrobles.com",
  "password": "Admin12345"
}
```

Respuesta esperada `200 OK`:

```json
{
  "tenantId": "22222222-2222-2222-2222-222222222222",
  "usuarioId": "11111111-1111-1111-1111-111111111111",
  "rol": "Administrador",
  "refreshToken": "TOKEN_GENERADO_POR_EL_BACKEND"
}
```

Errores esperados:

- `401 Unauthorized` si el correo no existe.
- `401 Unauthorized` si la password no coincide.
- `400 BadRequest` si el body viene mal formado.

Notas de prueba:

- Igual que en el registro, aqui tambien se escriben las cookies `muzu_token` y `muzu_refresh_token`.
- Debo guardar el `refreshToken` porque me sirve para probar refresh y logout.

## 3. Refresh token

Ruta:

```http
POST /api/auth/refresh-token
Content-Type: application/json
```

Body de ejemplo:

```json
{
  "refreshToken": "TOKEN_GENERADO_POR_EL_BACKEND"
}
```

Respuesta esperada `200 OK`:

```json
{
  "tenantId": "22222222-2222-2222-2222-222222222222",
  "usuarioId": "11111111-1111-1111-1111-111111111111",
  "rol": "Administrador",
  "refreshToken": "NUEVO_TOKEN_GENERADO_POR_EL_BACKEND"
}
```

Errores esperados:

- `401 Unauthorized` si el refresh token no existe.
- `401 Unauthorized` si el token ya fue revocado.
- `401 Unauthorized` si el token ya expiro.

Notas de prueba:

- Cuando hago refresh, el token anterior queda revocado.
- Para volver a usar el flujo debo quedarme con el nuevo `refreshToken`.

## 4. Logout

Ruta:

```http
POST /api/auth/logout
Content-Type: application/json
```

Body de ejemplo:

```json
{
  "refreshToken": "NUEVO_TOKEN_GENERADO_POR_EL_BACKEND"
}
```

Respuesta esperada:

- `204 No Content`

Notas de prueba:

- En logout se revoca el refresh token enviado.
- Tambien se eliminan las cookies `muzu_token` y `muzu_refresh_token`.

## Flujo recomendado para probar este modulo

1. Probar `register-tenant`.
2. Guardar `tenant.id`.
3. Probar `login`.
4. Guardar `refreshToken`.
5. Probar `refresh-token`.
6. Guardar el nuevo `refreshToken`.
7. Probar `logout`.

## Ejemplo rapido en Postman

Headers sugeridos:

- `Content-Type: application/json`

Coleccion sugerida:

- `Auth > Register Tenant`
- `Auth > Login`
- `Auth > Refresh Token`
- `Auth > Logout`

## Ejemplo rapido en cURL

```bash
curl -X POST "http://localhost:5177/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"correo\":\"admin@losrobles.com\",\"password\":\"Admin12345\"}"
```
