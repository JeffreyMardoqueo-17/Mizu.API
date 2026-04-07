# Documentacion Backend Muzu.Api

En esta carpeta deje documentado el backend de `Muzu.Api` despues del refactor.

Documentos disponibles:

- `arquitectura-backend.md`: explica la arquitectura, reglas de capas, inyeccion de dependencias, DTOs, servicios, repositorios y transacciones.
- `modulo-auth.md`: documentacion practica para probar `AuthController`.
- `modulo-config.md`: documentacion practica para probar `ConfigController`.
- `modulo-usuarios-roles.md`: gestion de usuarios, normalizacion de roles/permisos, tablas `docs` y `directiva`, y ejemplos de prueba para `UsuariosController`.
- `modulo-directivas.md`: endpoints y reglas de transicion atomica para directivas (`BoardsController`).
- `mejora-users-boards-auth.md`: detalle tecnico de endpoints, transacciones, constraints e integracion de seguridad temporal.

La idea es que aqui quede tanto la parte tecnica de arquitectura como la parte operativa para probar cada modulo del API.
