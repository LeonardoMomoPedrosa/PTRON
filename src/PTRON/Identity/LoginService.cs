using Microsoft.AspNetCore.Identity;
using PTRON.Models;

namespace PTRON.Identity;

public enum LoginOutcome
{
    Succeeded,
    InvalidCredentials,
    NotActivated,
    LockedOut,
}

public sealed record LoginResult(LoginOutcome Outcome, ApplicationUser? User = null);

/// <summary>
/// Checks e-mail (or the reserved "Leo" alias) and password. An account that is still pending only
/// gets told so after the password is right, so the message cannot be used to find registered e-mails.
/// </summary>
public sealed class LoginService
{
    private readonly UserManager<ApplicationUser> _users;

    public LoginService(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<LoginResult> LoginAsync(string? identifier, string? password)
    {
        var login = identifier?.Trim() ?? "";
        if (login.Length == 0 || string.IsNullOrEmpty(password))
            return new LoginResult(LoginOutcome.InvalidCredentials);

        var user = ReservedUsers.IsReservedName(login)
            ? await _users.FindByIdAsync(ReservedUsers.LeoId)
            : await _users.FindByEmailAsync(login);
        if (user is null)
            return new LoginResult(LoginOutcome.InvalidCredentials);

        if (await _users.IsLockedOutAsync(user))
            return new LoginResult(LoginOutcome.LockedOut);

        if (!await _users.CheckPasswordAsync(user, password))
        {
            await _users.AccessFailedAsync(user);
            return await _users.IsLockedOutAsync(user)
                ? new LoginResult(LoginOutcome.LockedOut)
                : new LoginResult(LoginOutcome.InvalidCredentials);
        }

        if (!await _users.IsEmailConfirmedAsync(user))
            return new LoginResult(LoginOutcome.NotActivated, user);

        await _users.ResetAccessFailedCountAsync(user);
        return new LoginResult(LoginOutcome.Succeeded, user);
    }
}
