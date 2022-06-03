namespace CloutCast.Models.Scanner
{
    public class BasicTransferTxIndexMetadata {
        public ulong TotalInputNanos { get; set; }
        public ulong TotalOutputNanos { get; set; }
        public ulong FeeNanos { get; set; }
        public string UtxoOpsDump { get; set; }
    }
}