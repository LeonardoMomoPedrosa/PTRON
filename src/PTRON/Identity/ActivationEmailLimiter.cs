using System.Collections.Concurrent;

namespace PTRON.Identity;

/// <summary>
/// Caps how many activation e-mails one address can receive in a rolling window, so the sign-up
/// and resend flows cannot be used to spam an inbox. State is in memory (single-process app),
/// so a restart resets the counters.
/// </summary>
public sealed class ActivationEmailLimiter
{
    public const int DefaultMaxEmails = 7;
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _sent = new();
    private readonly TimeProvider _time;
    private readonly int _maxEmails;
    private readonly TimeSpan _window;

    public ActivationEmailLimiter(TimeProvider time)
        : this(time, DefaultMaxEmails, DefaultWindow)
    {
    }

    public ActivationEmailLimiter(TimeProvider time, int maxEmails, TimeSpan window)
    {
        _time = time;
        _maxEmails = maxEmails;
        _window = window;
    }

    /// <summary>Registers an e-mail to <paramref name="email"/> and returns true, or false if the limit was reached.</summary>
    public bool TryAcquire(string email)
    {
        var now = _time.GetUtcNow();
        var key = email.Trim().ToUpperInvariant();
        var history = _sent.GetOrAdd(key, _ => new Queue<DateTimeOffset>());

        lock (history)
        {
            while (history.Count > 0 && now - history.Peek() >= _window)
                history.Dequeue();

            if (history.Count >= _maxEmails)
                return false;

            history.Enqueue(now);
            return true;
        }
    }
}
