using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PTRON.Identity;
using PTRON.Models;

namespace PTRON.Controllers;

[AllowAnonymous]
[Route("account")]
public sealed class AccountController : Controller
{
    private readonly LoginService _login;
    private readonly PasswordResetService _passwordReset;
    private readonly ChangePasswordService _changePassword;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAntiforgery _antiforgery;

    public AccountController(
        LoginService login,
        PasswordResetService passwordReset,
        ChangePasswordService changePassword,
        UserManager<ApplicationUser> users,
        IAntiforgery antiforgery)
    {
        _login = login;
        _passwordReset = passwordReset;
        _changePassword = changePassword;
        _users = users;
        _antiforgery = antiforgery;
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromForm] string? email)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Redirect("/esqueci-senha?error=expired");
        }

        await _passwordReset.RequestAsync(email, ClientIp.From(HttpContext), HttpContext.RequestAborted);
        return Redirect("/esqueci-senha?sent=1");
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromForm] string? currentPassword,
        [FromForm] string? newPassword,
        [FromForm] string? confirmPassword)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/login?returnUrl=" + Uri.EscapeDataString("/conta/senha"));

        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Redirect("/conta/senha?error=expired");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _changePassword.ChangeAsync(userId, currentPassword, newPassword, confirmPassword);
        if (!result.Succeeded)
            return Redirect(ChangePasswordUrl(result.Errors));

        var user = await _users.FindByIdAsync(userId!);
        if (user is null)
            return Redirect("/login?returnUrl=" + Uri.EscapeDataString("/conta/senha"));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthPrincipal.Create(user),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            });
        return Redirect("/conta/senha?ok=1");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] string? username,
        [FromForm] string? password,
        [FromForm] string? returnUrl)
    {
        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Redirect(LoginUrl("expired", safeReturnUrl));
        }

        var result = await _login.LoginAsync(username, password);
        switch (result.Outcome)
        {
            case LoginOutcome.Succeeded:
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    AuthPrincipal.Create(result.User!),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    });
                return Redirect(safeReturnUrl ?? "~/");

            case LoginOutcome.NotActivated:
                return Redirect(LoginUrl("inactive", safeReturnUrl, result.User!.Email));

            case LoginOutcome.LockedOut:
                return Redirect(LoginUrl("locked", safeReturnUrl));

            default:
                return Redirect(LoginUrl("1", safeReturnUrl));
        }
    }

    [HttpGet("logout")]
    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }

    private static string ChangePasswordUrl(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
            return "/conta/senha?erro=" + Uri.EscapeDataString("Não foi possível alterar a senha.");

        return "/conta/senha?" + string.Join("&", errors.Select(e => "erro=" + Uri.EscapeDataString(e)));
    }

    private static string LoginUrl(string error, string? returnUrl, string? email = null)
    {
        var url = "/login?error=" + Uri.EscapeDataString(error);
        if (returnUrl is not null)
            url += "&returnUrl=" + Uri.EscapeDataString(returnUrl);
        if (!string.IsNullOrEmpty(email))
            url += "&email=" + Uri.EscapeDataString(email);
        return url;
    }
}
