using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Api.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _host;
    private readonly int    _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _from;
    private readonly string _to;
    private readonly bool   _enabled;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger   = logger;
        _host     = Environment.GetEnvironmentVariable("SMTP_HOST")     ?? "";
        _port     = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;
        _username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
        _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
        _from     = Environment.GetEnvironmentVariable("SMTP_FROM")     ?? "";
        _to       = Environment.GetEnvironmentVariable("SMTP_TO")       ?? "";

        _enabled = !string.IsNullOrEmpty(_host) &&
                   !string.IsNullOrEmpty(_username) &&
                   !string.IsNullOrEmpty(_password) &&
                   !string.IsNullOrEmpty(_to);

        if (!_enabled)
            _logger.LogWarning("Email désactivé — variables SMTP_* non configurées.");
        else
            _logger.LogInformation("Email activé → {To}", _to);
    }

    public async Task SendAlertAsync(string subject, string body)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Email ignoré (non configuré) : {Subject}", subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_from));
            message.To.Add(MailboxAddress.Parse(_to));
            message.Subject = $"🚨 NetworkLogAnalyzer — {subject}";

            message.Body = new TextPart("html")
            {
                Text = $"""
                    <div style="font-family:sans-serif;max-width:600px;margin:auto">
                      <div style="background:#ef4444;padding:16px;border-radius:8px 8px 0 0">
                        <h2 style="color:white;margin:0">🚨 Alerte CRITICAL détectée</h2>
                      </div>
                      <div style="background:#f4f4f5;padding:20px;border-radius:0 0 8px 8px">
                        <p style="font-size:14px;color:#27272a">{body}</p>
                        <p style="font-size:12px;color:#71717a;margin-top:16px">
                          Généré par NetworkLogAnalyzer le {DateTime.Now:dd/MM/yyyy à HH:mm:ss}
                        </p>
                      </div>
                    </div>
                    """
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_username, _password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email envoyé : {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur envoi email : {Subject}", subject);
        }
    }
}
