namespace MelaFair.Web.Models.Entities;

/// <summary>
/// Stores a one-time password reset request. The token itself is never persisted.
/// </summary>
public class PasswordResetRequest
{
    public int PasswordResetRequestId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ApplicationUser User { get; set; } = null!;
}
