namespace Muzu.Api.Core.Models;

public sealed class BoardMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid RolId { get; set; }
    public string Cargo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
