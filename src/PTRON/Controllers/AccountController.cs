using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTRON.Identity;
using PTRON.Models;

namespace PTRON.Controllers;

[AllowAnonymous]
[Route("account")]
public sealed class AccountController : Controller
{
    private readonly LoginService _login;
    private readonly IAntiforgery _antiforgery;

    public AccountController(LoginService login, IAntiforgery antiforgery)
    {
        _login = login;
        _antiforgery = antiforgery;
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
                    CreatePrincipal(result.User!),
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

    private static ClaimsPrincipal CreatePrincipal(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Reservado ? user.UserName! : user.Email ?? user.UserName!),
        };
        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
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
