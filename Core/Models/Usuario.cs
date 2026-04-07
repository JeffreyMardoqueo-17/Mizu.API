using System;

namespace Muzu.Api.Core.Models
{
    public class Usuario
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string DUI { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public long? Niu { get; set; }
        public string Rol { get; set; } = "Socio";
        public bool Activo { get; set; } = true;
        public bool Eliminado { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaEliminacion { get; set; }
        public DateTime? TempPasswordGeneratedAt { get; set; }
        public DateTime? TempPasswordViewedAt { get; set; }
    }
}
