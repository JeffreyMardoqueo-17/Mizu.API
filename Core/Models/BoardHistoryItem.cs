namespace Muzu.Api.Core.Models;

public sealed class BoardHistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Guid? ActorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
