using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MelaFair.Web.Data;
using MelaFair.Web.Models.Entities;

namespace MelaFair.Web.Services;

public interface IPasswordResetService
{
    Task RequestAsync(string email, string resetUrlBase);
    Task<bool> IsValidAsync(string token);
    Task<IdentityResult> ResetAsync(string token, string newPassword);
}

public class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(20);
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        IEmailService emailService, ILogger<PasswordResetService> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task RequestAsync(string email, string resetUrlBase)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;

        var existingRequests = _context.PasswordResetRequests.Where(r => r.UserId == user.Id && r.UsedAt == null);
        _context.PasswordResetRequests.RemoveRange(existingRequests);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _context.PasswordResetRequests.Add(new PasswordResetRequest
        {
            UserId = user.Id,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.Add(TokenLifetime)
        });
        await _context.SaveChangesAsync();

        try
        {
            var separator = resetUrlBase.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            await _emailService.SendPasswordResetAsync(user.Email, $"{resetUrlBase}{separator}token={Uri.EscapeDataString(token)}");
        }
        catch (Exception ex)
        {
            // Do not log the email address, token, or reset URL.
            _logger.LogError(ex, "Password reset email delivery failed.");
        }
    }

    public Task<bool> IsValidAsync(string token) => FindValidRequestAsync(token).AnyAsync();

    public async Task<IdentityResult> ResetAsync(string token, string newPassword)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var request = await FindValidRequestAsync(token).Include(r => r.User).SingleOrDefaultAsync();
        if (request is null) return IdentityResult.Failed(new IdentityError { Description = "This password reset link is invalid, expired, or has already been used." });

        // Claim the request atomically so concurrent submissions cannot use the same link twice.
        var claimed = await _context.PasswordResetRequests
            .Where(r => r.PasswordResetRequestId == request.PasswordResetRequestId && r.UsedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.UsedAt, DateTime.UtcNow));
        if (claimed != 1) return IdentityResult.Failed(new IdentityError { Description = "This password reset link is invalid, expired, or has already been used." });

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(request.User);
        var result = await _userManager.ResetPasswordAsync(request.User, identityToken, newPassword);
        if (!result.Succeeded) return result;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private IQueryable<PasswordResetRequest> FindValidRequestAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return _context.PasswordResetRequests.Where(_ => false);
        var hash = HashToken(token);
        return _context.PasswordResetRequests.Where(r => r.TokenHash == hash && r.UsedAt == null && r.ExpiresAt > DateTime.UtcNow);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
