# Modulo Config

## Objetivo

En este modulo concentre la administracion de la configuracion del tenant.

Controller asociado:

- `ConfigController`

Ruta base:

- `/api/config`

URL local de prueba:

- `http://localhost:5177`
- `https://localhost:7227`

## Endpoints del modulo

- `GET /api/config/{tenantId}`
- `PUT /api/config/{tenantId}`

## Dato importante antes de probar

Para probar este modulo necesito un `tenantId` valido.

La forma mas facil de conseguirlo es:

1. Registrar un tenant con `POST /api/auth/register-tenant`.
2. Copiar el `tenant.id` que devuelve la respuesta.

## 1. Obtener configuracion del tenant

Ruta:

```http
GET /api/config/{tenantId}
```

Ejemplo real:

```http
GET /api/config/22222222-2222-2222-2222-222222222222
```

Respuesta esperada `200 OK`:

```json
{
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
```

Errores esperados:

- `404 NotFound` si el tenant no tiene configuracion o el `tenantId` no existe.

## 2. Actualizar configuracion del tenant

Ruta:

```http
PUT /api/config/{tenantId}
Content-Type: application/json
```

Ejemplo real:

```http
PUT /api/config/22222222-2222-2222-2222-222222222222
```

Body de ejemplo:

```json
{
  "moneda": "USD",
  "limiteConsumoFijo": 40,
  "precioConsumoFijo": 4,
  "limiteConsumoExtra1": 50,
  "porcentajeExtra1": 0.18,
  "limiteConsumoExtra2": 60,
  "porcentajeExtra2": 0.30,
  "multaRetraso": 3,
  "multaNoAsistirReunion": 7,
  "multaNoAsistirTrabajo": 12
}
```

Respuesta esperada `200 OK`:

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "tenantId": "22222222-2222-2222-2222-222222222222",
  "moneda": "USD",
  "limiteConsumoFijo": 40,
  "precioConsumoFijo": 4,
  "limiteConsumoExtra1": 50,
  "porcentajeExtra1": 0.18,
  "limiteConsumoExtra2": 60,
  "porcentajeExtra2": 0.30,
  "multaRetraso": 3,
  "multaNoAsistirReunion": 7,
  "multaNoAsistirTrabajo": 12
}
```

Errores esperados:

- `404 NotFound` si no existe configuracion para ese tenant.
- `400 BadRequest` si falta algun campo requerido.
- `400 BadRequest` si algun valor numerico no cumple la validacion.

## Flujo recomendado para probar este modulo

1. Crear tenant desde el modulo `Auth`.
2. Copiar el `tenant.id`.
3. Ejecutar `GET /api/config/{tenantId}` para ver la configuracion inicial.
4. Ejecutar `PUT /api/config/{tenantId}` con nuevos valores.
5. Ejecutar otra vez `GET /api/config/{tenantId}` para confirmar los cambios.

## Ejemplo rapido en Postman

Headers sugeridos para el update:

- `Content-Type: application/json`

Coleccion sugerida:

- `Config > Get Config`
- `Config > Update Config`

## Ejemplo rapido en cURL

```bash
curl -X PUT "http://localhost:5177/api/config/22222222-2222-2222-2222-222222222222" \
  -H "Content-Type: application/json" \
  -d "{\"moneda\":\"USD\",\"limiteConsumoFijo\":40,\"precioConsumoFijo\":4,\"limiteConsumoExtra1\":50,\"porcentajeExtra1\":0.18,\"limiteConsumoExtra2\":60,\"porcentajeExtra2\":0.30,\"multaRetraso\":3,\"multaNoAsistirReunion\":7,\"multaNoAsistirTrabajo\":12}"
```
