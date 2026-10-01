using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PTRON.Models;

namespace PTRON.Identity;

/// <summary>
/// Makes sure the reserved Leo account exists, is confirmed, carries the configured
/// email and has a password hash stored in the database.
/// </summary>
public static class LeoBootstrap
{
    /// <summary>
    /// Last-resort password, used only when the account has no password and
    /// PTRON_Leo__InitialPassword is not set, so access is never lost. Must be changed after first login.
    /// </summary>
    internal const string FallbackPassword = "Leoxp123";

    public static async Task EnsureAsync(IServiceProvider services, ILogger logger)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var hasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var options = services.GetRequiredService<IOptions<LeoOptions>>().Value;

        var email = options.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Leo:Email não está configurado.");
        }

        var leo = await users.FindByIdAsync(ReservedUsers.LeoId);
        var created = leo is null;
        if (leo is null)
        {
            leo = new ApplicationUser { Id = ReservedUsers.LeoId, UserName = ReservedUsers.LeoUserName };
        }

        var changed = created;

        if (!leo.Reservado) { leo.Reservado = true; changed = true; }
        if (!leo.EmailConfirmed) { leo.EmailConfirmed = true; changed = true; }

        if (!string.Equals(leo.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var other = await users.FindByEmailAsync(email);
            if (other is not null && other.Id != leo.Id)
            {
                throw new InvalidOperationException(
                    $"O e-mail do Leo ({email}) já pertence a outro usuário.");
            }

            leo.Email = email;
            leo.NormalizedEmail = users.NormalizeEmail(email);
            changed = true;
        }

        if (string.IsNullOrEmpty(leo.PasswordHash))
        {
            var initial = options.InitialPassword;
            if (string.IsNullOrWhiteSpace(initial))
            {
                initial = FallbackPassword;
                logger.LogWarning(
                    "PTRON_Leo__InitialPassword não definido: usando a senha padrão do Leo. Troque-a após o primeiro login.");
            }

            leo.PasswordHash = hasher.HashPassword(leo, initial);
            leo.SecurityStamp = Guid.NewGuid().ToString("N");
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        var result = created ? await users.CreateAsync(leo) : await users.UpdateAsync(leo);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Falha ao preparar o usuário Leo: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
