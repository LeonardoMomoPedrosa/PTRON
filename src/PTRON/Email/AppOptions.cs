namespace PTRON.Email;

public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public site origin used to build links inside e-mails, without a trailing path.</summary>
    public string? PublicBaseUrl { get; set; }
}
