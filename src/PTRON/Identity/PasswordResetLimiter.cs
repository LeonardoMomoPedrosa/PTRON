namespace PTRON.Identity;

/// <summary>
/// Caps password reset e-mails per address and per client IP. Both counters are consumed whether or
/// not the address belongs to an account, so the limit does not reveal which e-mails exist.
/// </summary>
public sealed class PasswordResetLimiter
{
    public const int MaxPerEmail = 5;
    public const int MaxPerIp = 20;
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly SlidingWindowLimiter _perEmail;
    private readonly SlidingWindowLimiter _perIp;

    public PasswordResetLimiter(TimeProvider time)
    {
        _perEmail = new SlidingWindowLimiter(time, MaxPerEmail, Window);
        _perIp = new SlidingWindowLimiter(time, MaxPerIp, Window);
    }

    public bool TryAcquire(string email, string? ip)
    {
        var ipAllowed = string.IsNullOrWhiteSpace(ip) || _perIp.TryAcquire(ip);
        var emailAllowed = _perEmail.TryAcquire(email);
        return ipAllowed && emailAllowed;
    }
}
