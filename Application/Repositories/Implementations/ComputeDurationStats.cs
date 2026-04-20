using Application.Contracts.Analytics;

namespace Application.Repositories.Implementations
{
    public static class DurationStatsHelper
    {
        public static DurationStats Compute(List<TimeSpan> sorted)
        {
            if (sorted.Count == 0)
                return new DurationStats(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);

            var avg = TimeSpan.FromTicks((long)sorted.Average(d => d.Ticks));
            var mid = sorted.Count / 2;
            var median = sorted.Count % 2 == 0
                ? TimeSpan.FromTicks((sorted[mid - 1].Ticks + sorted[mid].Ticks) / 2)
                : sorted[mid];

            return new DurationStats(avg, sorted.First(), sorted.Last(), median);
        }
    }
}