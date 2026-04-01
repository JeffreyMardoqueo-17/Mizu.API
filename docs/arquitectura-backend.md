# Arquitectura Backend Muzu.Api

## Objetivo del refactor

Hice este refactor porque el backend ya estaba funcionando, pero la estructura no era sostenible para seguir creciendo. Había varios problemas que quería corregir:

- Se estaban creando dependencias manualmente con `new`.
- Los controladores podían terminar hablando directo con repositorios.
- Las interfaces no estaban organizadas por responsabilidad.
- La conexión a base de datos no estaba resuelta de forma consistente.
- La lógica de negocio y la lógica de acceso a datos estaban demasiado mezcladas.

Mi intención con este cambio fue dejar una base más senior, más limpia y más escalable, siguiendo principios SOLID y buenas prácticas de desarrollo.

## Regla principal de arquitectura

La regla más importante que quiero mantener es esta:

- El `Controller` nunca toca un `Repository`.
- El `Controller` solo conoce interfaces de servicios.
- Los `Service` orquestan la lógica de negocio.
- Los `Repository` solo resuelven persistencia.
- Nadie debe depender de implementaciones concretas si puede depender de abstracciones.

En otras palabras:

- `Controller -> Service Interface`
- `Service -> Repository Interfaces`
- `Repository -> DbConnectionFactory`

## Estructura de carpetas que dejé

La organización quedó así:

- `Controllers/`
  Controladores HTTP. Solo reciben requests, validan el modelo implícitamente con ASP.NET y delegan al servicio.

- `Core/DTOs/`
  DTOs de entrada y salida. Aquí van los contratos que expone la API.

- `Core/Interfaces/`
  Interfaces de infraestructura y repositorios.

- `Core/Interfaces/Service/`
  Interfaces exclusivas de la capa de servicios.

- `Core/Mappers/`
  Extensiones de mapeo entre entidades y DTOs.

- `Core/Models/`
  Entidades del dominio.

- `Core/Repositories/`
  Implementaciones concretas del acceso a datos con Dapper.

- `Core/Services/`
  Implementaciones de la lógica de negocio.

- `Extensions/`
  Extensiones para registrar dependencias y mantener `Program.cs` más limpio.

## Como resolví el tema del singleton

Inicialmente la idea era que hubiera una sola conexión o una sola forma de instanciar la conexión. Después del refactor dejé una solución más correcta para backend web:

- `DbConnectionFactory` sí queda como una dependencia singleton.
- La conexión `IDbConnection` no queda singleton.

Esto es intencional.

En una API web no es buena práctica compartir una misma conexión física entre múltiples requests porque:

- no es thread-safe,
- puede generar errores de concurrencia,
- complica transacciones,
- rompe el aislamiento entre operaciones.

Entonces la decisión correcta fue:

- Tener una única fábrica central de conexiones.
- Crear conexiones por operación.
- Reutilizar la misma conexión solo cuando existe una transacción activa.

Con eso logro consistencia sin caer en el error de usar una conexión global compartida.

## Inyección de dependencias

Organicé el registro de dependencias en:

- `Extensions/DependencyInjectionExtensions.cs`

Ahí separé:

- `AddPersistence()`
- `AddApplicationServices()`

La intención es que `Program.cs` quede más limpio y que la composición del sistema esté en un solo lugar.

### Lifetimes que decidí usar

- `Singleton`
  Solo para servicios realmente estables y sin estado mutable por request, como `DbConnectionFactory` y `JwtService`.

- `Scoped`
  Para servicios y repositorios que participan en el flujo de una request, como `AuthService`, `TenantConfigService`, `UnitOfWork` y todos los repositorios.

No dejé repositorios como singleton porque eso no era consistente con el manejo de conexión ni con el ciclo de vida natural de una API.

## Controladores

Los controladores quedaron con una responsabilidad mínima.

### AuthController

Ahora `AuthController`:

- depende solo de `IAuthService`,
- no conoce repositorios,
- no construye objetos de infraestructura,
- solo coordina request, response y cookies.

Endpoints actuales:

- `POST /api/auth/register-tenant`
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

### ConfigController

Antes `ConfigController` dependía de `ITenantConfigRepository`, lo cual rompía la regla de capas.

Ahora depende de:

- `ITenantConfigService`

Eso deja respetado el principio de que el controlador jamás habla con persistencia directamente.

Endpoints actuales:

- `GET /api/config/{tenantId}`
- `PUT /api/config/{tenantId}`

## Servicios

### AuthService

`AuthService` quedó como el orquestador del caso de uso de autenticación.

Responsabilidades:

- registrar tenant y usuario administrador,
- validar login,
- refrescar tokens,
- revocar tokens en logout,
- coordinar repositorios,
- manejar el flujo transaccional cuando una operación necesita atomicidad.

El servicio ya no devuelve entidades crudas mezcladas de cualquier forma. Ahora devuelve DTOs y resultados pensados para la API.

### TenantConfigService

Creé este servicio para encapsular la lógica de configuración del tenant.

Responsabilidades:

- obtener configuración por tenant,
- actualizar configuración por tenant,
- mapear entidad a DTO de respuesta.

## Repositorios

Los repositorios quedaron enfocados exclusivamente en acceso a datos.

Características importantes:

- ya no hacen `new` de otras dependencias,
- ya no dependen de un singleton estático manual,
- reciben `IDbConnectionFactory` por constructor,
- soportan trabajar con `IDbTransaction` opcional,
- usan Dapper con `CommandDefinition`,
- abren conexión solo cuando hace falta.

Además creé `RepositoryBase` para centralizar el patrón de apertura de conexión y evitar repetir la misma lógica en todos los repositorios.

## Unit of Work

Reemplacé la versión anterior del `UnitOfWork`.

Antes:

- creaba repositorios con `new`,
- manejaba estado interno de conexión y transacción de forma poco flexible,
- estaba acoplado a implementaciones concretas.

Ahora:

- depende de `IDbConnectionFactory`,
- expone métodos transaccionales simples,
- permite ejecutar lógica dentro de una transacción sin que el servicio tenga que administrar manualmente apertura, commit y rollback.

Esto me sirve especialmente en procesos como:

- registro del primer tenant y usuario,
- refresh token,
- cualquier caso futuro donde varias escrituras deban ser atómicas.

## DTOs

Separé mejor los contratos de entrada y salida.

Archivos principales:

- `Core/DTOs/AuthDtos.cs`
- `Core/DTOs/TenantConfigDtos.cs`

Con esto busqué evitar:

- devolver entidades completas directamente,
- exponer información interna que no debería salir,
- acoplar la API al modelo de persistencia.

También agregué anotaciones de validación en varios DTOs (`Required`, `StringLength`, `EmailAddress`, `Range`, etc.) para fortalecer la entrada del API.

## Mapeo

Como quería mantener el uso de mapper de una forma ordenada, dejé los mapeos explícitos en:

- `Core/Mappers/DtoMappingExtensions.cs`

Ahí centralicé:

- DTO a entidad,
- entidad a DTO de respuesta,
- aplicación de cambios de update sobre una entidad existente.

De momento lo resolví con extensiones porque:

- es simple,
- explícito,
- fácil de mantener,
- evita meter AutoMapper si todavía no hace falta.

Si más adelante el volumen de mapeos crece bastante, se puede migrar a un mapper formal sin romper la arquitectura.

## Manejo de autenticación

También ordené mejor la parte de JWT:

- `IJwtService` quedó en `Core/Interfaces/Service/`
- `JwtService` quedó en `Core/Services/`

`JwtService` ahora toma configuración desde variables de entorno y también puede leer desde configuración de ASP.NET como respaldo.

## Decisiones de negocio importantes que dejé

### Registro de tenant

En el flujo de registro ahora:

- valido si el tenant ya existe,
- valido si el correo ya existe,
- creo tenant,
- creo configuración por defecto,
- creo usuario administrador,
- genero tokens,
- devuelvo una respuesta estructurada.

Todo esto queda dentro de una frontera transaccional para no dejar datos a medias.

### Login

En login:

- busco usuario por correo,
- valido password hash,
- genero access token,
- genero refresh token,
- devuelvo DTO limpio.

### Refresh token

En refresh:

- valido existencia del token,
- valido que no esté revocado,
- valido expiración,
- revoco el token anterior,
- emito uno nuevo.

## Convenciones que quiero mantener a partir de ahora

Estas son reglas que quiero seguir manteniendo en este backend:

- Ningún controller debe inyectar repositorios.
- Ningún service debe depender de clases concretas si existe una interfaz.
- Ningún repository debe contener lógica de negocio.
- No usar `new` manual para dependencias de aplicación.
- No usar conexiones compartidas globales.
- Si una operación toca varias escrituras relacionadas, debe evaluarse usar `UnitOfWork`.
- Las respuestas del API deben salir mediante DTOs.
- Las interfaces de servicios deben vivir solo en `Core/Interfaces/Service`.
- Las interfaces de repositorios deben vivir solo en `Core/Interfaces`.

## Como agregar un nuevo modulo sin romper la arquitectura

Si agrego una nueva funcionalidad, quiero seguir este orden:

1. Crear DTOs de request y response.
2. Crear o extender la interfaz del servicio.
3. Implementar la lógica del servicio.
4. Crear o extender la interfaz del repositorio si hace falta persistencia.
5. Implementar el repositorio.
6. Registrar dependencias en `DependencyInjectionExtensions.cs`.
7. Exponer el endpoint en el controller, dependiendo solo de la interfaz del servicio.

## Archivos principales del refactor

Estos son los archivos más importantes que quedaron después del cambio:

- `Program.cs`
- `Extensions/DependencyInjectionExtensions.cs`
- `Core/Interfaces/IDbConnectionFactory.cs`
- `Core/Interfaces/IUnitOfWork.cs`
- `Core/Interfaces/ITenantRepository.cs`
- `Core/Interfaces/IUsuarioRepository.cs`
- `Core/Interfaces/ITenantConfigRepository.cs`
- `Core/Interfaces/IRefreshTokenRepository.cs`
- `Core/Interfaces/IMultaRepository.cs`
- `Core/Interfaces/Service/IAuthService.cs`
- `Core/Interfaces/Service/ITenantConfigService.cs`
- `Core/Interfaces/Service/IJwtService.cs`
- `Core/Repositories/DbConnectionFactory.cs`
- `Core/Repositories/RepositoryBase.cs`
- `Core/Repositories/UnitOfWork.cs`
- `Core/Repositories/TenantRepository.cs`
- `Core/Repositories/UsuarioRepository.cs`
- `Core/Repositories/TenantConfigRepository.cs`
- `Core/Repositories/RefreshTokenRepository.cs`
- `Core/Repositories/MultaRepository.cs`
- `Core/Services/AuthService.cs`
- `Core/Services/TenantConfigService.cs`
- `Core/Services/JwtService.cs`
- `Core/DTOs/AuthDtos.cs`
- `Core/DTOs/TenantConfigDtos.cs`
- `Core/Mappers/DtoMappingExtensions.cs`
- `Controllers/AuthController.cs`
- `Controllers/ConfigController.cs`

## Estado actual

Después del refactor, el backend quedó compilando correctamente.

Lo importante para mí no es solo que compile, sino que ahora ya tengo una base mucho más mantenible para seguir creciendo:

- con capas claras,
- con responsabilidades separadas,
- con DI organizada,
- con contratos explícitos,
- con menos acoplamiento,
- y con mejor posibilidad de escalar el proyecto sin convertirlo en un bloque difícil de mantener.

## Actualizacion: modulo usuarios, roles y permisos

Se agrego una normalizacion adicional para control de acceso:

- `roles`
- `permisos`
- `rol_permisos`
- `usuario_roles`

Y para nuevos requerimientos funcionales:

- `docs` para guardar DUI u otros documentos por usuario,
- `directiva` para periodos de junta directiva,
- `directiva_miembros` para asociar usuarios y cargos por periodo.

Con esto, los roles dejan de depender de un campo libre en `usuarios` y pasan a un esquema relacional que permite:

- controlar permisos por rol,
- cambiar roles sin romper historial,
- proteger reglas de negocio como no permitir auto-democion del ultimo `Administrador` o `Presidente`.

El endpoint nuevo de gestion quedo en `UsuariosController` y mantiene la misma filosofia de arquitectura:

- `Controller -> IUsuarioAdministracionService`
- `Service -> IUsuarioRepository + IRolRepository + IUnitOfWork`
- `Repository -> IDbConnectionFactory`
