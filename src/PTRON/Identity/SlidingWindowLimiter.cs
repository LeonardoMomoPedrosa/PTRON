using System.Collections.Concurrent;

namespace PTRON.Identity;

/// <summary>Allows at most N events per key in a rolling window. In memory, so a restart resets it.</summary>
public sealed class SlidingWindowLimiter
{
    private const int PruneThreshold = 5000;

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _events = new();
    private readonly TimeProvider _time;
    private readonly int _max;
    private readonly TimeSpan _window;

    public SlidingWindowLimiter(TimeProvider time, int max, TimeSpan window)
    {
        _time = time;
        _max = max;
        _window = window;
    }

    public bool TryAcquire(string key)
    {
        var now = _time.GetUtcNow();
        if (_events.Count > PruneThreshold)
            Prune(now);

        var history = _events.GetOrAdd(key.Trim().ToUpperInvariant(), _ => new Queue<DateTimeOffset>());
        lock (history)
        {
            while (history.Count > 0 && now - history.Peek() >= _window)
                history.Dequeue();

            if (history.Count >= _max)
                return false;

            history.Enqueue(now);
            return true;
        }
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var (key, history) in _events)
        {
            lock (history)
            {
                while (history.Count > 0 && now - history.Peek() >= _window)
                    history.Dequeue();

                if (history.Count == 0)
                    _events.TryRemove(key, out _);
            }
        }
    }
}
