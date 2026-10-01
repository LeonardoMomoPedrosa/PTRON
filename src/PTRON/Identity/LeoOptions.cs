namespace PTRON.Identity;

public sealed class LeoOptions
{
    public const string SectionName = "Leo";

    public string Email { get; set; } = "pedrosa.leonardo@gmail.com";

    /// <summary>
    /// Password applied only when the reserved user has no password yet
    /// (env var PTRON_Leo__InitialPassword).
    /// </summary>
    public string? InitialPassword { get; set; }
}
