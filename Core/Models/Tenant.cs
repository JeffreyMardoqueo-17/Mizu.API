using System;
using System.Collections.Generic;

namespace Muzu.Api.Core.Models
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public virtual List<Usuario> Usuarios { get; set; } = new();
        public virtual TenantConfig Configuracion { get; set; } = new();
    }
}
