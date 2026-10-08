using System.Security.Claims;

namespace PTRON.Identity;

public static class ReservedUsers
{
    public const string LeoId = "leo";
    public const string LeoUserName = "Leo";
    public const string PolicyName = "Reserved";
    public const string ReservedClaimType = "ptron:reservado";

    public static bool IsReservedPrincipal(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        if (string.Equals(user.FindFirstValue(ReservedClaimType), "true", StringComparison.Ordinal))
            return true;

        return user.FindFirstValue(ClaimTypes.NameIdentifier) == LeoId;
    }

    public static bool IsReservedName(string? value)
        => string.Equals(value?.Trim(), LeoUserName, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when a regular account may not use this e-mail: the "leo" name or the Leo account e-mail.</summary>
    public static bool IsReservedEmail(string? email, string? leoEmail)
        => IsReservedName(email)
           || (!string.IsNullOrWhiteSpace(leoEmail)
               && string.Equals(email?.Trim(), leoEmail.Trim(), StringComparison.OrdinalIgnoreCase));
}
