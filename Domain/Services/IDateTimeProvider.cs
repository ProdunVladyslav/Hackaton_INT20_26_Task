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

public sealed class FakeDateTimeProvider(DateTime fakeUtcNow) : IDateTimeProvider
{
    public DateTime UtcNow => fakeUtcNow;
    public DateOnly Today => DateOnly.FromDateTime(fakeUtcNow);
}
