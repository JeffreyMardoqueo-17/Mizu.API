using System.Net;
using System.Net.Mail;
using Muzu.Api.Core.Interfaces.Service;

namespace Muzu.Api.Core.Services;

public sealed class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderPassword;
    private readonly string _frontendUrl;

    public EmailService()
    {
        _smtpHost = Environment.GetEnvironmentVariable("EMAIL_HOST")
            ?? "smtp.gmail.com";

        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("EMAIL_PORT"), out var envPort)
            ? envPort
            : 587;

        _senderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER")
            ?? throw new InvalidOperationException("No se encontro EMAIL_SENDER.");

        _senderPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
            ?? throw new InvalidOperationException("No se encontro EMAIL_PASSWORD.");

        _frontendUrl = (Environment.GetEnvironmentVariable("MUZU_FRONTEND_URL")
            ?? "http://localhost:3000").TrimEnd('/');
    }

    public async Task SendBoardMemberAssignedAsync(
        string emailReceptor,
        string nombreUsuario,
        string rol,
        string nombreDirectiva,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailReceptor))
        {
            throw new InvalidOperationException("El usuario no tiene correo configurado para notificacion.");
        }

        var asunto = $"Asignacion confirmada a directiva: {nombreDirectiva}";
        var bodyHtml = $@"
<html>
    <body style='font-family: Arial, sans-serif; color: #222; background-color: #f7f8fa; padding: 20px;'>
        <div style='max-width: 680px; margin: 0 auto; background: white; border-radius: 10px; padding: 24px; border: 1px solid #e5e7eb;'>
            <h2 style='margin-top: 0; color: #0f766e;'>Asignacion de Directiva Confirmada</h2>
            <p>Hola <strong>{WebUtility.HtmlEncode(nombreUsuario)}</strong>,</p>
            <p>Tu asignacion a la directiva <strong>{WebUtility.HtmlEncode(nombreDirectiva)}</strong> fue registrada.</p>

            <div style='background: #f0fdfa; border: 1px solid #99f6e4; border-radius: 8px; padding: 14px; margin: 18px 0;'>
                <p style='margin: 0 0 6px 0;'><strong>Rol:</strong> {WebUtility.HtmlEncode(rol)}</p>
                <p style='margin: 0;'><strong>Periodo:</strong> {fechaInicio:yyyy-MM-dd} al {fechaFin:yyyy-MM-dd}</p>
            </div>

            <p style='margin-top: 16px;'>Cuando la directiva se active, recibiras un segundo correo con tus credenciales de acceso.</p>
            <p style='margin-top: 24px; color: #6b7280;'>Este mensaje fue generado automaticamente por Muzu.</p>
        </div>
    </body>
</html>";

                await SendEmailAsync(emailReceptor, asunto, bodyHtml, cancellationToken);
        }

        public async Task SendBoardMemberCredentialsAsync(
                string emailReceptor,
                string nombreUsuario,
                string rol,
                string nombreDirectiva,
                DateOnly fechaInicio,
                DateOnly fechaFin,
                string password,
                CancellationToken cancellationToken = default)
        {
                if (string.IsNullOrWhiteSpace(emailReceptor))
                {
                        throw new InvalidOperationException("El usuario no tiene correo configurado para notificacion.");
                }

                var asunto = $"Credenciales de acceso para directiva: {nombreDirectiva}";
                var loginUrl = $"{_frontendUrl}/login";

                var bodyHtml = $@"
<html>
    <body style='font-family: Arial, sans-serif; color: #222; background-color: #f7f8fa; padding: 20px;'>
        <div style='max-width: 680px; margin: 0 auto; background: white; border-radius: 10px; padding: 24px; border: 1px solid #e5e7eb;'>
            <h2 style='margin-top: 0; color: #0f766e;'>Acceso a Directiva</h2>
            <p>Hola <strong>{WebUtility.HtmlEncode(nombreUsuario)}</strong>,</p>
            <p>Tu rol dentro de la directiva <strong>{WebUtility.HtmlEncode(nombreDirectiva)}</strong> ya fue actualizado.</p>

            <div style='background: #f0fdfa; border: 1px solid #99f6e4; border-radius: 8px; padding: 14px; margin: 18px 0;'>
                <p style='margin: 0 0 6px 0;'><strong>Rol:</strong> {WebUtility.HtmlEncode(rol)}</p>
                <p style='margin: 0;'><strong>Periodo:</strong> {fechaInicio:yyyy-MM-dd} al {fechaFin:yyyy-MM-dd}</p>
            </div>

            <h3 style='margin-bottom: 8px;'>Credenciales de acceso</h3>
            <p style='margin: 0 0 6px 0;'><strong>Usuario:</strong> {WebUtility.HtmlEncode(emailReceptor)}</p>
            <p style='margin: 0 0 6px 0;'><strong>Contraseña:</strong> {WebUtility.HtmlEncode(password)}</p>
            <p style='margin-top: 10px;'>Usa estas credenciales durante todo el periodo de la directiva. No se requiere cambio obligatorio al ingresar.</p>

            <p style='margin-top: 16px;'>Ingresa desde: <a href='{WebUtility.HtmlEncode(loginUrl)}'>{WebUtility.HtmlEncode(loginUrl)}</a></p>
            <p style='margin-top: 24px; color: #6b7280;'>Este mensaje fue generado automaticamente por Muzu.</p>
        </div>
    </body>
</html>";

                await SendEmailAsync(emailReceptor, asunto, bodyHtml, cancellationToken);
        }

        public async Task SendBoardActivationCredentialsAsync(
        string emailReceptor,
        string nombreUsuario,
        string rol,
        string nombreDirectiva,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailReceptor))
        {
            throw new InvalidOperationException("El usuario no tiene correo configurado para notificacion.");
        }

        var asunto = $"Directiva activa: credenciales de acceso para {nombreDirectiva}";
        var loginUrl = $"{_frontendUrl}/login";

        var bodyHtml = $@"
<html>
  <body style='font-family: Arial, sans-serif; color: #222; background-color: #f7f8fa; padding: 20px;'>
    <div style='max-width: 680px; margin: 0 auto; background: white; border-radius: 10px; padding: 24px; border: 1px solid #e5e7eb;'>
    <h2 style='margin-top: 0; color: #0f766e;'>Activacion de Directiva</h2>
      <p>Hola <strong>{WebUtility.HtmlEncode(nombreUsuario)}</strong>,</p>
            <p>La directiva <strong>{WebUtility.HtmlEncode(nombreDirectiva)}</strong> ya esta activa y tu acceso fue habilitado.</p>

      <div style='background: #f0fdfa; border: 1px solid #99f6e4; border-radius: 8px; padding: 14px; margin: 18px 0;'>
                <p style='margin: 0 0 6px 0;'><strong>Rol:</strong> {WebUtility.HtmlEncode(rol)}</p>
        <p style='margin: 0 0 6px 0;'><strong>Periodo:</strong> {fechaInicio:yyyy-MM-dd} al {fechaFin:yyyy-MM-dd}</p>
      </div>

      <h3 style='margin-bottom: 8px;'>Credenciales de acceso</h3>
      <p style='margin: 0 0 6px 0;'><strong>Usuario:</strong> {WebUtility.HtmlEncode(emailReceptor)}</p>
      <p style='margin: 0 0 6px 0;'><strong>Contrasena temporal:</strong> {WebUtility.HtmlEncode(temporaryPassword)}</p>
    <p style='margin-top: 10px;'>La contrasena se mantiene durante todo el periodo de esta directiva. No se solicitara cambio obligatorio al ingresar.</p>

      <p style='margin-top: 16px;'>Ingresa desde: <a href='{WebUtility.HtmlEncode(loginUrl)}'>{WebUtility.HtmlEncode(loginUrl)}</a></p>
      <p style='margin-top: 24px; color: #6b7280;'>Este mensaje fue generado automaticamente por Muzu.</p>
    </div>
  </body>
</html>";

        await SendEmailAsync(emailReceptor, asunto, bodyHtml, cancellationToken);
    }

    private async Task SendEmailAsync(string recipient, string subject, string bodyHtml, CancellationToken cancellationToken)
    {
        using var smtp = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_senderEmail, _senderPassword)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_senderEmail),
            Subject = subject,
            Body = bodyHtml,
            IsBodyHtml = true
        };

        message.To.Add(recipient);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(message);
        }
        catch (Exception ex) when (ex is SmtpException || ex is InvalidOperationException || ex is FormatException)
        {
            throw new InvalidOperationException("No se pudo enviar el correo.", ex);
        }
    }
}
