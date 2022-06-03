namespace CloutCast.Contracts
{
    public interface IPromotionHeader
    {
        decimal BitCloutToUsdRate  { get; }
        int Duration { get; } //minutes
        long Fee { get; }
        long Engagements { get; }
        long Rate { get; }
    }
}