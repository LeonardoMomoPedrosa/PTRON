using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using PTRON.Email;
using PTRON.Models;

namespace PTRON.Identity;

public sealed record PasswordResetResult(bool Succeeded, bool InvalidLink, IReadOnlyList<string> Errors)
{
    public static PasswordResetResult Ok { get; } = new(true, false, Array.Empty<string>());
    public static PasswordResetResult Invalid { get; } = new(false, true, Array.Empty<string>());
}

public sealed class PasswordResetService
{
    private const string ResetPasswordPurpose = "ResetPassword";

    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppEmailSender _email;
    private readonly PasswordResetLimiter _limiter;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        UserManager<ApplicationUser> users,
        IAppEmailSender email,
        PasswordResetLimiter limiter,
        ILogger<PasswordResetService> logger)
    {
        _users = users;
        _email = email;
        _limiter = limiter;
        _logger = logger;
    }

    /// <summary>
    /// Sends a reset link when the address belongs to an activated account. Always completes the same
    /// way, so the caller cannot tell whether the address exists, is pending, or was rate limited.
    /// </summary>
    public async Task RequestAsync(string? email, string? clientIp, CancellationToken cancellationToken = default)
    {
        var address = email?.Trim() ?? "";
        if (address.Length == 0 || address.Length > SignUpService.MaxEmailLength
            || !MailAddress.TryCreate(address, out _))
            return;

        if (!_limiter.TryAcquire(address, clientIp))
        {
            _logger.LogWarning("Limite de redefinição de senha atingido (IP {Ip}).", clientIp);
            return;
        }

        try
        {
            var user = await _users.FindByEmailAsync(address);
            if (user is null || !user.EmailConfirmed)
                return;

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            await _email.SendPasswordResetAsync(user.Email!, user.Id, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o e-mail de redefinição de senha.");
        }
    }

    /// <summary>Checks the link without using it, so the page can warn before the user types a password.</summary>
    public async Task<bool> IsLinkValidAsync(string? userId, string? token)
    {
        var user = await FindResettableAsync(userId, token);
        return user is not null
               && await _users.VerifyUserTokenAsync(
                   user, PasswordResetTokenProvider.ProviderName, ResetPasswordPurpose, token!);
    }

    public async Task<PasswordResetResult> ResetAsync(string? userId, string? token, string? newPassword)
    {
        var user = await FindResettableAsync(userId, token);
        if (user is null)
            return PasswordResetResult.Invalid;

        var result = await _users.ResetPasswordAsync(user, token!, newPassword ?? "");
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                return PasswordResetResult.Invalid;

            return new PasswordResetResult(false, false, result.Errors.Select(e => e.Description).Distinct().ToList());
        }

        // Proving control of the mailbox also lifts a lockout from too many wrong passwords.
        await _users.SetLockoutEndDateAsync(user, null);
        await _users.ResetAccessFailedCountAsync(user);

        try
        {
            await _email.SendPasswordChangedAsync(user.Email!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o aviso de senha alterada para o usuário {UserId}.", user.Id);
        }

        return PasswordResetResult.Ok;
    }

    private async Task<ApplicationUser?> FindResettableAsync(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return null;

        var user = await _users.FindByIdAsync(userId);
        return user is not null && user.EmailConfirmed ? user : null;
    }
}
