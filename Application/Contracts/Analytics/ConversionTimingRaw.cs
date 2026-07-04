namespace Application.Contracts.Analytics
{
    public sealed record ConversionTimingRaw(
        double MedianSeconds,
        double AvgSeconds,
        double PctWithin24Hours,   // 0.0 - 1.0
        int SampleSize
    );
}
