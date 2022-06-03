namespace CloutCast.Models
{
    [JetBrains.Annotations.UsedImplicitly]
    public class StakeEntryStats
    {
        public ulong TotalStakeNanos { get; set; }
        public ulong TotalStakeOwedNanos { get; set; }
        public ulong TotalCreatorEarningsNanos { get; set; }
        public ulong TotalFeesBurnedNanos { get; set; }
        public ulong TotalPostStakeNanos { get; set; }
    }
}