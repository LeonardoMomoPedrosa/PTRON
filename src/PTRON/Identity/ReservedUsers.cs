namespace PTRON.Identity;

public static class ReservedUsers
{
    public const string LeoId = "leo";
    public const string LeoUserName = "Leo";

    public static bool IsReservedName(string? value)
        => string.Equals(value?.Trim(), LeoUserName, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when a regular account may not use this e-mail: the "leo" name or the Leo account e-mail.</summary>
    public static bool IsReservedEmail(string? email, string? leoEmail)
        => IsReservedName(email)
           || (!string.IsNullOrWhiteSpace(leoEmail)
               && string.Equals(email?.Trim(), leoEmail.Trim(), StringComparison.OrdinalIgnoreCase));
}
