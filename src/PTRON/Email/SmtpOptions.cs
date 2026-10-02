namespace PTRON.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>StartTls, SslOnConnect, None, or StartTlsWhenAvailable.</summary>
    public string SecureSocketOptions { get; set; } = "StartTls";

    public string? User { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "Makelectron";

    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
