# Módulo de Documentos de Partners - Arquitectura y Flujo

## 📋 Descripción General

El módulo de documentos de partners proporciona funcionalidad completa para la carga, visualización, actualización y eliminación de documentos de identidad (DUI, Pasaporte, Cédula, Licencia) de los socios/usuarios. Implementa un sistema robusto con validaciones en múltiples capas, integración con Cloudinary para almacenamiento en la nube, y manejo de eliminación lógica.

### Características Principales

- ✅ **Máximo 2 documentos por usuario** - Validación estricta de límites
- ✅ **Integración Cloudinary** - Almacenamiento seguro en la nube con eliminación automática
- ✅ **CRUD Completo** - Create, Read, Update, Delete con eliminación lógica
- ✅ **Validaciones Robustas** - Tipos de archivos, tamaño, format ode documento
- ✅ **Seguridad Multi-tenant** - Aislamiento de datos por tenant
- ✅ **Performance Optimized** - Índices estratégicos en PostgreSQL
- ✅ **API RESTful** - Endpoints bem diseñados con HTTP correcto

## 🏗️ Arquitectura

### Estructura de Capas

```
Presentation Layer (API Controller)
          ↓
   PartnersController
          ↓
Business Logic Layer
          ↓
   IPartnerDocumentService
   PartnerDocumentService
          ↓
Data Access Layer
          ↓
   IPartnerDocumentRepository
   PartnerDocumentRepository
          ↓
PostgreSQL Database
   partner_documents table
          ↓
External Services
   Cloudinary API
```

### Modelos y DTOs

#### Modelo de Dominio: PartnerDocument

```csharp
public class PartnerDocument
{
    public Guid Id { get; set; }                    // ID único del documento
    public Guid UsuarioId { get; set; }             // FK al usuario/socio
    public Guid TenantId { get; set; }              // FK al tenant (multi-tenancy)
    public string DocumentUrl { get; set; }         // URL segura en Cloudinary
    public string CloudinaryPublicId { get; set; }  // ID para eliminar de Cloudinary
    public string DocumentType { get; set; }        // DUI | Pasaporte | Cédula | Licencia
    public string FileName { get; set; }            // Nombre original
    public long FileSizeBytes { get; set; }         // Tamaño para auditoría
    public int DisplayOrder { get; set; }           // 1 o 2 (máximo 2 documentos)
    public bool Activo { get; set; }                // Flag de actividad
    public DateTime FechaCreacion { get; set; }     // Timestamp de creación
    public DateTime? FechaActualizacion { get; set; } // Última actualización
    public DateTime? FechaEliminacion { get; set; } // Eliminación lógica
    public bool Eliminado { get; set; }             // Flag de eliminación lógica
}
```

#### DTOs (Data Transfer Objects)

1. **CreatePartnerDocumentRequestDto** - Para inicial carga
   - UsuarioId: Guid
   - DocumentType: string (validado contra conjunto)
   - FileName: string

2. **UpdatePartnerDocumentRequestDto** - Para actualizar metadatos
   - DocumentType: string - Cambiar tipo sin re-subir imagen
   - DisplayOrder: int (1 o 2)

3. **PartnerDocumentDto** - Respuesta unitaria
   - Incluye toda información pública del documento
   - No expone URLs internas ni IDs de Cloudinary

4. **PartnerDocumentsListDto** - Respuesta batch
   - UsuarioId
   - Documents: IReadOnlyList<PartnerDocumentDto>
   - TotalCount: int
   - ActiveCount: int

5. **PartnerDocumentUploadResponseDto** - Respuesta de carga
   - Document: PartnerDocumentDto
   - Message: string

6. **PartnerDocumentDeleteResponseDto** - Respuesta de eliminación
   - DocumentId: Guid
   - Message: string

## 🔄 Flujo de Operaciones

### 1. Obtener Documentos de un Usuario

**GET /api/partners/{partnerUserId}/documents**

```
Cliente HTTP Request
        ↓
PartnersController.ObtenerDocumentosAsync()
        ↓
Validar contexto de autenticación
        ↓
IPartnerDocumentService.ObtenerDocumentosUsuarioAsync()
        ↓
Validar que usuario existe (IUsuarioRepository)
        ↓
IPartnerDocumentRepository.ObtenerPorUsuarioIdAsync()
        ↓
Query SQL: SELECT FROM partner_documents WHERE usuario_id = @UsuarioId AND eliminado = FALSE
        ↓
Mapear a DTOs
        ↓
Responder PartnerDocumentsListDto (200 OK)
```

**Validaciones:**
- Token JWT válido y activo
- Usuario debe ser el propietario o administrador
- Usuario must exist en base de datos

**Indexes utilizados:**
- `idx_partner_documents_usuario` - Búsqueda rápida por usuario

### 2. Cargar Nuevo Documento

**POST /api/partners/{partnerUserId}/documents** (multipart/form-data)

```
Cliente envía archivo + metadata
        ↓
PartnersController.CargarDocumentoAsync()
        ↓
Validar formato del request (form data)
        ↓
IPartnerDocumentService.CargarDocumentoAsync()
        ↓
├─ ValidarArchivo()
│  ├─ Validar que archivo no está vacío
│  ├─ Validar tamaño ≤ 10MB
│  ├─ Validar extensión (.jpg, .jpeg, .png, .gif, .webp)
│  └─ Throw si falsa validación
│
├─ ValidarTipoDocumento()
│  ├─ Whitelist: DUI, Pasaporte, Cédula, Licencia
│  └─ Throw si no permitido
│
├─ ValidarOrdenDocumento()
│  ├─ Validar que orden está entre 1-2
│  └─ Throw si inválido
│
├─ Validar usuario existe
│  └─ ObtenerPorIdAsync(usuarioId)
│
├─ Validar límite de documentos (máx 2)
│  └─ ContarDocumentosActivosAsync(usuarioId)
│  └─ Throw si count >= 2
│
├─ Validar no hay conflicto de orden
│  └─ ObtenerPorUsuarioIdAsync() y verificar DisplayOrder
│
├─ CargarACloudinaryAsync()
│  ├─ Abrir stream del archivo
│  ├─ Crear ImageUploadParams con carpeta: partners/{usuarioId}/documents
│  ├─ Subir a Cloudinary
│  └─ Obtener SecureUrl y PublicId
│
├─ Crear objeto PartnerDocument
│  └─ Poblar todos los campos
│
└─ IPartnerDocumentRepository.CrearAsync()
   ├─ INSERT INTO partner_documents
   └─ Retornar documento guardado

Mapear a PartnerDocumentUploadResponseDto
        ↓
Responder (201 Created) con Location header
```

**Validaciones en 3 capas:**
- **Controlador:** HTTP Content-Type, método HTTP
- **Servicio:** Lógica de negocio (límites, tipos permitidos)
- **Repositorio:** Integridad referencial en BD

**Cloudinary Integration:**
```
Folder structure: partners/{usuarioId}/documents
Public ID pattern: {DocumentType}_{RandomGuid:N}
Secure URL: Usada en responses y almacenada en BD
Transformaciones: Configuradas pero no aplicadas en upload básico
```

### 3. Reemplazar Documento

**PUT /api/partners/documents/{documentId}/replace** (multipart/form-data)

```
Cliente envía nuevo archivo
        ↓
PartnersController.ReemplazarDocumentoAsync()
        ↓
IPartnerDocumentService.ReemplazarDocumentoAsync()
        ↓
├─ ValidarArchivo() - Mismas validaciones que upload
│
├─ Obtener documento existente
│  ├─ ObtenerPorIdAsync(documentId)
│  ├─ Validar que existe
│  └─ Validar que pertenece al tenant
│
├─ Eliminar imagen anterior de Cloudinary
│  ├─ Usar CloudinaryPublicId almacenado
│  ├─ Ejecutar DeletionParams en background (no bloquear si falla)
│  └─ Log de error pero no fail
│
├─ Cargar nueva imagen a Cloudinary
│  └─ CargarACloudinaryAsync()
│
├─ Actualizar documento con nueva URL y PublicId
│  └─ ActualizarAsync()
│
└─ Retornar PartnerDocumentUploadResponseDto

Responder (200 OK)
```

**Consideraciones:**
- Si Cloudinary falla al eliminar anterior, se log pero no se cancela operación
- Si falla al cargar nueva, se rollback completo (no actualizar BD)
- Mantiene el mismo ID - es actualización, no nueva creación

### 4. Actualizar Metadatos

**PATCH /api/partners/documents/{documentId}**

```
Cliente envía JSON: { documentType, displayOrder }
        ↓
PartnersController.ActualizarDocumentoAsync()
        ↓
IPartnerDocumentService.ActualizarDocumentoAsync()
        ↓
├─ ValidarTipoDocumento()
├─ ValidarOrdenDocumento()
│
├─ Obtener documento
│  └─ ObtenerPorIdAsync(documentId)
│
├─ Validar propiedad del documento
│  └─ documento.TenantId == tenantId
│
├─ Validar no hay conflicto de orden con otros documentos
│  ├─ Obtener todos documentos del usuario
│  ├─ Verificar que no hay otro documento activo en mismo orden
│  └─ Permitir si es el mismo documento
│
└─ IPartnerDocumentRepository.ActualizarAsync()
   ├─ UPDATE partner_documents SET document_type, display_order, fecha_actualizacion
   └─ WHERE id = @Id AND eliminado = FALSE

Responder PartnerDocumentDto (200 OK)
```

$$\text{Query actualiza solo si} \begin{cases} id = \text{documentId} \\ eliminado = \text{FALSE} \end{cases}$$

### 5. Eliminar Documento

**DELETE /api/partners/documents/{documentId}**

```
Cliente solicita eliminación
        ↓
PartnersController.EliminarDocumentoAsync()
        ↓
IPartnerDocumentService.EliminarDocumentoAsync()
        ↓
├─ Obtener documento
│  └─ ObtenerPorIdAsync(documentId)
│
├─ Validar que existe y pertenece al tenant
│
├─ Eliminar de Cloudinary
│  ├─ Usar CloudinaryPublicId
│  ├─ Ejecutar DestroyAsync en background
│  └─ Log si falla pero continuar
│
└─ Eliminación lógica en BD
   ├─ UPDATE partner_documents SET eliminado = TRUE, activo = FALSE, fecha_eliminacion = NOW()
   └─ WHERE id = @Id

Responder PartnerDocumentDeleteResponseDto (200 OK)
```

**Eliminación Lógica vs Física:**
- Se usa soft delete para mantener auditoría
- Documentos eliminados nunca se retornan en operaciones read
- Flag `eliminado = TRUE` y fecha de eliminación registrada
- Permite recuperación si es necesario (sin implementar en esta fase)

## 🔒 Seguridad

### Principios Implementados

1. **Multi-Tenancy Isolation**
   - Cada documento bound a TenantId específico
   - Queries siempre filtran por TenantId
   - No se expone información entre tenants

2. **Authorization**
   - Solo propietario o Administrador puede acceder
   - Controlador valida `actorUserId` contra `partnerId`
   - Roles chequeados en claims del JWT

3. **Input Validation**
   - Whitelist de tipos de documento
   - Whitelist de extensiones de archivo
   - Tamaño máximo 10MB
   - Sanitización de nombres de archivo

4. **Cloudinary Security**
   - URL HTTPS (SecureUrl siempre)
   - Organización por tenant/usuario en carpetas
   - Public IDs únicos y hard to guess
   - Permisos: DELETE requiere API key (backend solo)

5. **CORS Protection**
   - Frontend origins configuradas
   - Credentials: include en requests
   - Backend valida origin

## 📊 Modelo de Datos

### Tabla: partner_documents

```sql
CREATE TABLE partner_documents (
    id UUID PRIMARY KEY,
    usuario_id UUID NOT NULL REFERENCES usuarios(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    document_url TEXT NOT NULL,
    cloudinary_public_id VARCHAR(500) NOT NULL,
    document_type VARCHAR(50) DEFAULT 'DUI',
    file_name VARCHAR(500) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    display_order SMALLINT CHECK (display_order >= 1 AND display_order <= 2),
    activo BOOLEAN DEFAULT TRUE,
    fecha_creacion TIMESTAMP DEFAULT NOW(),
    fecha_actualizacion TIMESTAMP,
    fecha_eliminacion TIMESTAMP,
    eliminado BOOLEAN DEFAULT FALSE,
    UNIQUE (usuario_id, display_order) WHERE NOT eliminado
);
```

### Índices

```sql
-- Búsqueda por usuario
CREATE INDEX idx_partner_documents_usuario 
  ON partner_documents(usuario_id) 
  WHERE NOT eliminado;

-- Búsqueda por tenant (para reports)
CREATE INDEX idx_partner_documents_tenant 
  ON partner_documents(tenant_id) 
  WHERE NOT eliminado;

-- Búsqueda por Cloudinary ID (para sincronización)
CREATE INDEX idx_partner_documents_cloudinary_public_id 
  ON partner_documents(cloudinary_public_id);

-- Ordenamiento temporal
CREATE INDEX idx_partner_documents_fecha_creacion 
  ON partner_documents(fecha_creacion DESC);
```

## 🎯 Endpoints API

### 1. Obtener Documentos
```
GET /api/partners/{partnerUserId}/documents

Headers:
  Authorization: Bearer {jwt_token}

Response (200 OK):
{
  "usuarioId": "guid",
  "documents": [
    {
      "id": "guid",
      "documentUrl": "https://res.cloudinary.com/...",
      "documentType": "DUI",
      "displayOrder": 1,
      "activo": true
    }
  ],
  "totalCount": 1,
  "activeCount": 1
}
```

### 2. Obtener por ID
```
GET /api/partners/documents/{documentId}

Response (200 OK):
{
  "id": "guid",
  "usuarioId": "guid",
  "documentUrl": "https://...",
  ...
}
```

### 3. Cargar Documento
```
POST /api/partners/{partnerUserId}/documents
Content-Type: multipart/form-data

Form Fields:
  - archivo: File (image/*) - max 10MB
  - tipoDocumento: "DUI" | "Pasaporte" | "Cédula" | "Licencia"
  - orden: 1 | 2

Response (201 Created):
Location: /api/partners/documents/{documentId}
{
  "document": { ... },
  "message": "Documento cargado exitosamente."
}
```

### 4. Reemplazar Documento
```
PUT /api/partners/documents/{documentId}/replace
Content-Type: multipart/form-data

Form Fields:
  - archivoNuevo: File (image/*)

Response (200 OK):
{
  "document": { ... },
  "message": "Documento reemplazado exitosamente."
}
```

### 5. Actualizar Metadatos
```
PATCH /api/partners/documents/{documentId}
Content-Type: application/json

Body:
{
  "documentType": "Pasaporte",
  "displayOrder": 2
}

Response (200 OK):
{ ... updated document ... }
```

### 6. Eliminar Documento
```
DELETE /api/partners/documents/{documentId}

Response (200 OK):
{
  "documentId": "guid",
  "message": "Documento eliminado exitosamente."
}
```

## ⚡ Performance Considerations

1. **Query Optimization**
   - Índice compound en (usuario_id, eliminado) para búsquedas rápidas
   - No usar JOIN innecesarios
   - Paginación: actualmente no implementada pero puede agregarse

2. **Cloudinary**
   - Upload en background sería óptimo pero request actual espera
   - Considerar colas de trabajo (Hangfire) en futuro
   - Batch deletes de múltiples documentos

3. **Caching**
   - Redis cache para listados (TTL 5-10 min)
   - Invalidar en CREATE, UPDATE, DELETE
   - ETag headers para cliente-side caching

4. **Database**
   - Connection pooling en appsettings
   - Transacciones para operaciones multi-step
   - Prepared statements (Dapper lo hace automático)

## 🐛 Manejo de Errores

### Errores Controlados

| Código | Escenario | Acción |
|--------|-----------|--------|
| 400    | Archivo inválido | Return BadRequest |
| 401    | No autenticado | Return Unauthorized |
| 403    | Usuario no es propietario | Return Forbid |
| 404    | Usuario/Documento no existe | Return NotFound |
| 409    | Conflicto (límite, duplicado orden) | Return Conflict |
| 500    | Error interno | Log + Return 500 |

### Reintentos

- **Cloudinary API:** Sin reintento automático en esta versión
- **BD:** Dapper maneja connection pooling pero sin retry logic
- **Futuro:** Implementar Polly para reintentos

## 📝 Configuración Requerida

### Environment Variables

```bash
# Backend
URL__Cloudinary=cloudinary://API_KEY:API_SECRET@CLOUD_NAME

# Docker Compose
URL__Cloudinary=${URL__Cloudinary}
```

### appsettings.json (opcional)

```json
{
  "Cloudinary": {
    "Url": "cloudinary://..."
  }
}
```

## 🔄 Integración con Dockerfile

```dockerfile
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Variable de entorno para runtime
ENV DOTNET_RUNNING_IN_CONTAINER=true
# URL__Cloudinary debe pasarse en compose o deploy

ENTRYPOINT ["dotnet", "Muzu.Api.dll"]
```

## 📚 Próximas Mejoras

1. **Paginación** de documentos (si > 2 es crítico)
2. **Compresión** de imágenes en Cloudinary
3. **Watermarks** para seguridad de documentos
4. **OCR** para validar contenido de DUI
5. **Audit logging** completo de operaciones
6. **Rate limiting** en uploads
7. **Email notifications** cuando se cargan documentos
8. **Admin approval workflow** antes de aceptar

---

**Versión:** 1.0  
**Fecha:** 2026-04-06  
**Autor:** Sistema Muzu  
**Estado:** Production Ready
