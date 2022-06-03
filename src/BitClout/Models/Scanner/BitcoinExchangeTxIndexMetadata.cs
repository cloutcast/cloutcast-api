namespace CloutCast.Models.Scanner
{
    public class BitcoinExchangeTxIndexMetadata
    {
        public string BitcoinSpendAddress { get; set; }
        public ulong SatoshisBurned { get; set; }
        public ulong NanosCreated { get; set; }
        public ulong TotalNanosPurchasedBefore { get; set; }
        public ulong TotalNanosPurchasedAfter { get; set; }
        public string BitcoinTxnHash { get; set; }
    }
}