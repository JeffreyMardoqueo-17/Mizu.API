using System;

namespace Muzu.Api.Core.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
        public bool Revocado { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
