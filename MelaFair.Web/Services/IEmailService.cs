namespace MelaFair.Web.Services;

public interface IEmailService
{
    Task SendPasswordResetAsync(string recipientEmail, string resetUrl);
}
