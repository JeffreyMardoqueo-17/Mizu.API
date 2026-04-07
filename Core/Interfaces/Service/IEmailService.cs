namespace Muzu.Api.Core.Interfaces.Service;

public interface IEmailService
{
    Task SendBoardMemberAssignedAsync(
        string emailReceptor,
        string nombreUsuario,
        string rol,
        string nombreDirectiva,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        CancellationToken cancellationToken = default);

    Task SendBoardMemberCredentialsAsync(
        string emailReceptor,
        string nombreUsuario,
        string rol,
        string nombreDirectiva,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string password,
        CancellationToken cancellationToken = default);

    Task SendBoardActivationCredentialsAsync(
        string emailReceptor,
        string nombreUsuario,
        string rol,
        string nombreDirectiva,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
