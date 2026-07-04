namespace Application.Contracts.Analytics
{
    public sealed record TierDistributionRaw(
        string Tier,   // "Hot" | "Warm" | "Cold" | "Disqualified"
        int Count
    );
}
