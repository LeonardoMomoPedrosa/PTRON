using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using PTRON.Models;

namespace PTRON.Identity;

/// <summary>Cookie principal for a signed-in user. The security stamp lets other sessions be rejected after a password change.</summary>
public static class AuthPrincipal
{
    public static ClaimsPrincipal Create(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Reservado ? user.UserName! : user.Email ?? user.UserName!),
        };

        if (!string.IsNullOrEmpty(user.SecurityStamp))
            claims.Add(new Claim(SessionStampValidator.StampClaimType, user.SecurityStamp));

        if (user.Reservado)
            claims.Add(new Claim(ReservedUsers.ReservedClaimType, "true"));

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
