using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PTRON.Data;

namespace PTRON.Identity;

/// <summary>
/// A cookie stays valid only while it carries the user's current security stamp and the account is activated.
/// Changing the password rotates the stamp, so every other session fails this check.
/// </summary>
public sealed class SessionStampValidator
{
    public const string StampClaimType = "AspNet.Identity.SecurityStamp";

    private readonly DbContextOptions<AppDbContext> _options;
    private readonly ILogger<SessionStampValidator> _logger;

    public SessionStampValidator(DbContextOptions<AppDbContext> options, ILogger<SessionStampValidator> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<bool> IsValidAsync(string? userId, string? stamp, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(stamp))
            return false;

        await using var db = new AppDbContext(_options);
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.SecurityStamp, u.EmailConfirmed })
            .FirstOrDefaultAsync(cancellationToken);

        return user is not null && user.EmailConfirmed && user.SecurityStamp == stamp;
    }

    public static async Task RejectIfStampMismatchAsync(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue(StampClaimType);
        var validator = context.HttpContext.RequestServices.GetRequiredService<SessionStampValidator>();

        bool valid;
        try
        {
            valid = await validator.IsValidAsync(userId, stamp, context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            validator._logger.LogError(ex, "Não foi possível validar a sessão. O cookie atual foi mantido.");
            return;
        }

        if (valid)
            return;

        if (!string.IsNullOrEmpty(userId))
            validator._logger.LogInformation("Sessão recusada para o usuário {UserId}.", userId);

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
