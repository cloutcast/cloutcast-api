namespace CloutCast.Models
{
    [JetBrains.Annotations.UsedImplicitly]
    public class CoinEntry
    {
        public ulong CreatorBasisPoints { get; set; }
        public ulong BitCloutLockedNanos { get; set; }
        public ulong NumberOfHolders { get; set; }
        public ulong CoinsInCirculationNanos { get; set; }
        public ulong CoinWatermarkNanos { get; set; }
    }
}