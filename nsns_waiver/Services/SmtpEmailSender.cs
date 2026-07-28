using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using nsns_waiver.Models;
using nsns_waiver.Options;

namespace nsns_waiver.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(
        EmailOutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateConfiguration();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.BodyHtml,
            IsBodyHtml = true
        };
        mailMessage.To.Add(new MailAddress(message.RecipientEmail));

        using var smtpClient = new SmtpClient(
            _options.Smtp.Host,
            _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.UseStartTls,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(
                _options.Smtp.Username,
                _options.Smtp.Password)
        };

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Smtp.Host)
            || _options.Smtp.Port is < 1 or > 65535
            || string.IsNullOrWhiteSpace(_options.Smtp.Username)
            || string.IsNullOrWhiteSpace(_options.Smtp.Password)
            || !MailAddress.TryCreate(_options.FromAddress, out _))
        {
            throw new InvalidOperationException(
                "Email SMTP configuration is incomplete or invalid.");
        }
    }
}
