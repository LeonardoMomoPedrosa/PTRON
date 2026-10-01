namespace PTRON.Identity;

public static class ReservedUsers
{
    public const string LeoId = "leo";
    public const string LeoUserName = "Leo";

    public static bool IsReservedName(string? value)
        => string.Equals(value?.Trim(), LeoUserName, StringComparison.OrdinalIgnoreCase);
}
