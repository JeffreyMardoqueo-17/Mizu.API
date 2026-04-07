# Diagrama de Flujo - Módulo de Documentos de Partners

```mermaid
sequenceDiagram
    autonumber
    
    participant Cliente as 🖥️ Cliente<br/>(Frontend)
    participant API as 🔌 PartnersController<br/>(API)
    participant Servicio as 💼 PartnerDocumentService<br/>(Business Logic)
    participant Repo as 🗄️ PartnerDocumentRepository<br/>(Data Access)
    participant BD as 📊 PostgreSQL<br/>partner_documents
    participant Cloud as ☁️ Cloudinary<br/>(Image Storage)

    Note over Cliente,Cloud: FLUJO 1: OBTENER DOCUMENTOS DE UN USUARIO
    
    Cliente->>API: GET /api/partners/{partnerId}/documents
    API->>API: Validar JWT Token
    API->>API: Validar autorización (propietario/admin)
    API->>Servicio: ObtenerDocumentosUsuarioAsync(usuarioId, tenantId)
    
    Servicio->>Repo: ObtenerPorUsuarioIdAsync(usuarioId)
    Repo->>BD: SELECT * FROM partner_documents<br/>WHERE usuario_id=@id AND eliminado=FALSE
    BD-->>Repo: [PartnerDocument[]]
    Repo-->>Servicio: [PartnerDocument[]]
    
    Servicio->>Servicio: Mapear a PartnerDocumentDto[]
    Servicio-->>API: PartnerDocumentsListDto
    
    API-->>Cliente: 200 OK - PartnerDocumentsListDto

    Note over Cliente,Cloud: FLUJO 2: CARGAR NUEVO DOCUMENTO
    
    Cliente->>API: POST /api/partners/{partnerId}/documents<br/>(multipart/form-data)
    API->>API: ValidarRequest() - Content-Type multipart
    API->>Servicio: CargarDocumentoAsync(partnerId, file, tipo, orden)
    
    Servicio->>Servicio: ValidarArchivo()
    alt Validación de archivo falla
        Servicio-->>API: ArgumentException
        API-->>Cliente: 400 Bad Request
    end
    
    Servicio->>Servicio: ValidarTipoDocumento()
    Servicio->>Servicio: ValidarOrdenDocumento()
    
    Servicio->>Repo: ObtenerPorIdAsync(usuarioId)
    Repo->>BD: SELECT * FROM usuarios WHERE id=@id
    BD-->>Repo: Usuario?
    Repo-->>Servicio: Usuario?
    
    alt Usuario no existe
        Servicio-->>API: InvalidOperationException
        API-->>Cliente: 404 Not Found
    end
    
    Servicio->>Repo: ContarDocumentosActivosAsync(usuarioId)
    Repo->>BD: SELECT COUNT(*) FROM partner_documents<br/>WHERE usuario_id=@id AND activo=TRUE
    BD-->>Repo: int
    Repo-->>Servicio: int
    
    alt Límite de 2 documentos alcanzado
        Servicio-->>API: InvalidOperationException<br/>("máximo 2 documentos")
        API-->>Cliente: 409 Conflict
    end
    
    Servicio->>Cloud: UploadAsync(ImageUploadParams)
    alt Upload a Cloudinary falla
        Cloud-->>Servicio: ImageUploadResult with Error
        Servicio-->>API: InvalidOperationException
        API-->>Cliente: 500 Internal Server Error
    end
    
    Cloud-->>Servicio: ImageUploadResult<br/>(SecureUrl, PublicId)
    
    Servicio->>Servicio: Crear PartnerDocument object
    Servicio->>Repo: CrearAsync(documento)
    
    Repo->>BD: INSERT INTO partner_documents<br/>VALUES (...)
    BD-->>Repo: void
    Repo-->>Servicio: PartnerDocument
    
    Servicio-->>API: PartnerDocumentUploadResponseDto
    API-->>Cliente: 201 Created<br/>Location: /api/partners/documents/{id}

    Note over Cliente,Cloud: FLUJO 3: REEMPLAZAR DOCUMENTO
    
    Cliente->>API: PUT /api/partners/documents/{docId}/replace<br/>(multipart/form-data)
    API->>Servicio: ReemplazarDocumentoAsync(docId, archivo)
    
    Servicio->>Servicio: ValidarArchivo()
    Servicio->>Repo: ObtenerPorIdAsync(docId)
    Repo->>BD: SELECT * WHERE id=@id
    BD-->>Repo: PartnerDocument?
    Repo-->>Servicio: PartnerDocument?
    
    alt Documento no existe
        Servicio-->>API: InvalidOperationException
        API-->>Cliente: 404 Not Found
    end
    
    Servicio->>Cloud: DestroyAsync(CloudinaryPublicId)
    Note over Servicio,Cloud: ⚠️ No bloquea si falla <br/>(log pero continuar)
    
    Servicio->>Cloud: UploadAsync(nuevo archivo)
    Cloud-->>Servicio: ImageUploadResult
    
    Servicio->>Repo: ActualizarAsync(documento actualizado)
    Repo->>BD: UPDATE partner_documents<br/>SET document_url, cloudinary_public_id<br/>WHERE id=@id
    BD-->>Repo: bool
    Repo-->>Servicio: bool
    
    Servicio-->>API: PartnerDocumentUploadResponseDto
    API-->>Cliente: 200 OK

    Note over Cliente,Cloud: FLUJO 4: ACTUALIZAR METADATOS
    
    Cliente->>API: PATCH /api/partners/documents/{docId}<br/>{documentType, displayOrder}
    API->>Servicio: ActualizarDocumentoAsync(docId, dto)
    
    Servicio->>Repo: ObtenerPorIdAsync(docId)
    Repo->>BD: SELECT * WHERE id=@id
    BD-->>Repo: PartnerDocument?
    Repo-->>Servicio: PartnerDocument?
    
    Servicio->>Repo: ActualizarAsync(documento)
    Repo->>BD: UPDATE SET document_type, display_order
    BD-->>Repo: bool
    Repo-->>Servicio: bool
    
    Servicio-->>API: PartnerDocumentDto
    API-->>Cliente: 200 OK

    Note over Cliente,Cloud: FLUJO 5: ELIMINAR DOCUMENTO
    
    Cliente->>API: DELETE /api/partners/documents/{docId}
    API->>Servicio: EliminarDocumentoAsync(docId, tenantId)
    
    Servicio->>Repo: ObtenerPorIdAsync(docId)
    Repo->>BD: SELECT * WHERE id=@id
    BD-->>Repo: PartnerDocument?
    Repo-->>Servicio: PartnerDocument?
    
    par Paralelo
        Servicio->>Cloud: DestroyAsync(CloudinaryPublicId)
        Note over Cloud: Eliminar imagen en background
    and Eliminar de BD
        Servicio->>Repo: EliminarAsync(docId)
        Repo->>BD: UPDATE SET eliminado=TRUE<br/>WHERE id=@id
        BD-->>Repo: bool
        Repo-->>Servicio: bool
    end
    
    Servicio-->>API: PartnerDocumentDeleteResponseDto
    API-->>Cliente: 200 OK
```

## Diagrama de Estados

```mermaid
stateDiagram-v2
    [*] --> SinDocumentos: Usuario creado
    
    SinDocumentos --> Document o1Activo: Cargar documento 1
    Document o1Activo --> Documento2Activo: Cargar documento 2
    Documento2Activo --> Documento1Inactivo: Desactivar doc 1
    Documento1Inactivo --> SinDocumentos: Eliminar doc 1 y 2
    
    Document o1Activo --> SinDocumentos: Eliminar documento 1
    Documento2Activo --> Document o1Activo: Eliminar documento 2
    
    Document o1Activo --> Document o1Actualizado: Reemplazar imagen
    Document o1Actualizado --> Document o1Activo: Operación completa
    
    SinDocumentos --> [*]
    
    note right of SinDocumentos
        Usuario sin documentos
        (válido, no obligatorio)
    end note
    
    note right of Document o1Activo
        1 documento cargado y activo
    end note
    
    note right of Documento2Activo
        2 documentos (máximo permitido)
    end note
```

## Diagrama de Relaciones de Entidades

```mermaid
erDiagram
    USUARIOS ||--o{ PARTNER_DOCUMENTS : has
    TENANTS ||--o{ PARTNER_DOCUMENTS : contains
    
    USUARIOS {
        uuid id
        uuid tenant_id
        string nombre
        string apellido
        string dui
        string correo
        string telefono
        string direccion
        string password_hash
        string rol
        boolean activo
        boolean eliminado
        datetime fecha_creacion
        datetime fecha_actualizacion
    }
    
    PARTNER_DOCUMENTS {
        uuid id
        uuid usuario_id FK
        uuid tenant_id FK
        text document_url
        varchar cloudinary_public_id
        varchar document_type
        varchar file_name
        bigint file_size_bytes
        smallint display_order
        boolean activo
        datetime fecha_creacion
        datetime fecha_actualizacion
        datetime fecha_eliminacion
        boolean eliminado
    }
    
    TENANTS {
        uuid id
        varchar nombre
        text direccion
        text logo_url
        datetime fecha_creacion
    }
```

## Flujo de Validación en Carga de Documento

```mermaid
flowchart TD
    A["POST /api/partners/{id}/documents"] --> B["ValidarArchivo()"]
    B --> B1{"¿Archivo<br/>válido?"}
    B1 -->|No| C["❌ 400 Bad Request"]
    B1 -->|Sí| D["ValidarTipoDocumento()"]
    
    D --> D1{"¿Tipo en<br/>whitelist?"}
    D1 -->|No| C
    D1 -->|Sí| E["ValidarOrdenDocumento()"]
    
    E --> E1{"¿Orden<br/>1 o 2?"}
    E1 -->|No| C
    E1 -->|Sí| F["¿Usuario existe?"]
    
    F --> F1{"¿Encontrado?"}
    F1 -->|No| G["❌ 404 Not Found"]
    F1 -->|Sí| H["ContarDocumentosActivos()"]
    
    H --> H1{"¿Count<br/>&lt; 2?"}
    H1 -->|No| I["❌ 409 Conflict<br/>Límite alcanzado"]
    H1 -->|Sí| J["¿Sin conflicto de orden?"]
    
    J --> J1{"¿DisplayOrder<br/>único?"}
    J1 -->|No| I
    J1 -->|Sí| K["Subir a Cloudinary"]
    
    K --> K1{"¿Upload<br/>exitoso?"}
    K1 -->|No| G
    K1 -->|Sí| L["Crear en BD"]
    
    L --> L1{"¿Insert<br/>exitoso?"}
    L1 -->|No| G
    L1 -->|Sí| M["✅ 201 Created"]
    
    style M fill:#4CAF50
    style C fill:#FF9800
    style G fill:#F44336
    style I fill:#FF9800
```

## Arquitectura de Capas

```mermaid
graph TB
    subgraph Presentation["Presentation Layer"]
        PC["PartnersController"]
    end
    
    subgraph Business["Business Logic Layer"]
        SI["IPartnerDocumentService"]
        SS["PartnerDocumentService"]
        SI -.implement.- SS
    end
    
    subgraph DataAccess["Data Access Layer"]
        RI["IPartnerDocumentRepository"]
        RS["PartnerDocumentRepository"]
        RI -.implement.- RS
    end
    
    subgraph Database["Persistence Layer"]
        PD["PostgreSQL<br/>partner_documents"]
    end
    
    subgraph External["External Services"]
        CDN["Cloudinary API<br/>Image Storage"]
    end
    
    PC -->|calls| SI
    SS -->|uses| RI
    RS -->|queries| PD
    RS -->|calls| CDN
    SS -->|calls| CDN
    
    UM["Usuario Model<br/>+<br/>IUsuarioRepository"]
    SS -->|validates| UM
    RS -->|references| UM
    
    style PC fill:#e3f2fd
    style SI fill:#f3e5f5
    style SS fill:#f3e5f5
    style RI fill:#e8f5e9
    style RS fill:#e8f5e9
    style PD fill:#fff3e0
    style CDN fill:#fcf8e3
    style UM fill:#fce4ec
```

---

**Nota:** Estos diagramas están diseñados para ser explicados en una entrevista técnica como Mid/Senior Level. Demuestran:
- ✅ Comprensión de arquitectura en capas
- ✅ Flujos claros de datos
- ✅ Validaciones en múltiples niveles
- ✅ Manejo robusto de errores
- ✅ Integraciones externas (Cloudinary)
- ✅ Consideraciones de seguridad y multi-tenancy
