using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace MelaFair.Web.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;

    public SmtpEmailService(IOptions<EmailOptions> options) => _options = options.Value;

    public async Task SendPasswordResetAsync(string recipientEmail, string resetUrl)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Email is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = "Reset your MELA Fair password",
            Body = $"<p>We received a request to reset your MELA Fair password.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(resetUrl)}\">Reset your password</a></p><p>This link expires in 20 minutes and can only be used once. If you did not request this, you can safely ignore this email.</p>",
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.UserName, _options.Password)
        };

        await client.SendMailAsync(message);
    }
}
