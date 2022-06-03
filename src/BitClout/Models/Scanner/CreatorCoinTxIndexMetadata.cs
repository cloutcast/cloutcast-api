namespace CloutCast.Models.Scanner
{
    public class CreatorCoinTxIndexMetadata
    {
        public string CreatorUsername { get; set; }
        public string OperationType { get; set; } // buy, 
        public ulong BitCloutToSellNanos { get; set; }
        public ulong CreatorCoinToSellNanos { get; set; }
        public ulong BitCloutToAddNanos { get; set; }
    }
}