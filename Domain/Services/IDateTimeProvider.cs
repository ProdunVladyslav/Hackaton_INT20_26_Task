namespace Domain.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}

/// <summary>
/// Frozen-calendar clock for manual testing — but "frozen" doesn't mean every
/// read returns the exact same instant. Each <see cref="UtcNow"/> read walks
/// the clock forward by a random step (5s-2min by default), so session and
/// answer durations come out non-zero and roughly human-paced instead of
/// collapsing to 0 the way a truly fixed instant would. The walk starts at
/// the configured date, so you still get a deterministic, pinned calendar day
/// for testing date-range filters — it just doesn't stay perfectly still.
///
/// Registered as a Singleton (see Program.cs), so the walk is shared and
/// monotonically increasing across every request for the process lifetime —
/// exactly what lets consecutive UserAnswer/UserSession timestamps produce
/// real deltas even though nothing is touching the actual system clock.
/// </summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    private readonly object _lock = new();
    private readonly int _minStepSeconds;
    private readonly int _maxStepSeconds;
    private DateTime _current;

    public FakeDateTimeProvider(DateTime startUtc, int minStepSeconds = 5, int maxStepSeconds = 120)
    {
        if (minStepSeconds < 0 || maxStepSeconds < minStepSeconds)
            throw new ArgumentOutOfRangeException(nameof(maxStepSeconds), "Require 0 <= minStepSeconds <= maxStepSeconds.");

        _current = startUtc;
        _minStepSeconds = minStepSeconds;
        _maxStepSeconds = maxStepSeconds;
    }

    public DateTime UtcNow
    {
        get
        {
            lock (_lock)
            {
                // Random.Shared is thread-safe on its own, but the read-then-write
                // of _current still needs the lock to stay monotonic under
                // concurrent requests.
                var stepSeconds = Random.Shared.Next(_minStepSeconds, _maxStepSeconds + 1);
                _current = _current.AddSeconds(stepSeconds);
                return _current;
            }
        }
    }

    // Deliberately doesn't advance the clock — asking "what day is it" isn't
    // an action that should itself consume a time step.
    public DateOnly Today
    {
        get
        {
            lock (_lock)
            {
                return DateOnly.FromDateTime(_current);
            }
        }
    }
}
