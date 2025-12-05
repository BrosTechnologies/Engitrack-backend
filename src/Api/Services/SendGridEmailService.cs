using SendGrid;
using SendGrid.Helpers.Mail;

namespace Engitrack.Api.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string code)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("SendGrid API key is not configured. Set 'SendGrid:ApiKey' in appsettings or 'SENDGRID_API_KEY' environment variable.");
            throw new InvalidOperationException("SendGrid API key is not configured.");
        }

        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "roll.pe2341@gmail.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "Engitrack";

        var subject = "EngiTrack - Código de recuperación de contraseña";
        
        var plainTextContent = $"Tu código de verificación es: {code}. Este código expirará en 15 minutos.";
        
        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; }}
        .container {{ max-width: 500px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #2196F3; }}
        .code {{ font-size: 32px; font-weight: bold; text-align: center; color: #333; background: #f0f0f0; padding: 15px; border-radius: 8px; letter-spacing: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #888; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2 class='header'>🔐 Recuperación de Contraseña</h2>
        <p>Hola,</p>
        <p>Has solicitado restablecer tu contraseña en EngiTrack. Usa el siguiente código de verificación:</p>
        <div class='code'>{code}</div>
        <p>Este código expirará en <strong>15 minutos</strong>.</p>
        <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
        <div class='footer'>
            <p>© 2025 EngiTrack - Gestión de Proyectos de Construcción</p>
        </div>
    </div>
</body>
</html>";

        try
        {
            var client = new SendGridClient(apiKey);
            
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);
            
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Password reset email sent to {Email} via SendGrid. StatusCode: {StatusCode}", toEmail, response.StatusCode);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogWarning("SendGrid returned non-success status. StatusCode: {StatusCode}, Body: {Body}", response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email} via SendGrid", toEmail);
            throw;
        }
    }
}
