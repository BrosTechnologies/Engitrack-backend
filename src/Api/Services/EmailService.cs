using System.Net;
using System.Net.Mail;

namespace Engitrack.Api.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string code);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string code)
    {
        var smtpServer = _configuration["Email:SmtpServer"];
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var appPassword = _configuration["Email:AppPassword"];

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword))
        {
            _logger.LogWarning("Email configuration is missing. Code: {Code} for {Email}", code, toEmail);
            return;
        }

        var subject = "EngiTrack - Código de recuperación de contraseña";
        var body = $@"
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
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(senderEmail, appPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            throw;
        }
    }
}
