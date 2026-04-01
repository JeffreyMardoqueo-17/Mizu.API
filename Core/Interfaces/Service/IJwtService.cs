namespace Muzu.Api.Core.Interfaces.Service;

public interface IJwtService
{
    string GenerarToken(Guid usuarioId, Guid tenantId, string rol);
}
