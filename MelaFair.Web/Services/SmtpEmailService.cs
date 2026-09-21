using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace MelaFair.Web.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string recipientEmail, string resetUrl)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Email service is not configured. Please check SMTP settings.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName ?? "MELA Fair", _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "Reset your MELA Fair password";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"<p>We received a request to reset your MELA Fair password.</p>" +
                       $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(resetUrl)}\">Reset your password</a></p>" +
                       $"<p>This link expires in 20 minutes and can only be used once. If you did not request this, you can safely ignore this email.</p>"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = _options.UseSsl
            ? SecureSocketOptions.Auto
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions);

        if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        _logger.LogInformation("Password reset email dispatched successfully to {Recipient}", recipientEmail);
    }
}
