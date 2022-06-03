using System.Collections.Generic;

namespace CloutCast.Models.Scanner
{
    public class Transactions {
        public string TransactionIDBase58Check { get; set; }
        public string RawTransactionHex { get; set; }
        public List<Inputs> Inputs { get; set; }
        public List<Outputs> Outputs { get; set; }
        public string SignatureHex { get; set; }
        public string TransactionType { get; set; }
        public string BlockHashHex { get; set; }
        public TransactionMetadata TransactionMetadata { get; set; }

        public bool Is(BitCloutTransactionTypes txType) => TransactionType == $"{txType}";
    }
}