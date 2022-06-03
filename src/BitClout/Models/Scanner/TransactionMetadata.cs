using System;
using System.Collections.Generic;

namespace CloutCast.Models.Scanner
{
    public class TransactionMetadata {
        public string BlockHashHex { get; set; }
        public string TransactorPublicKeyBase58Check { get; set; }

        public bool IsTransactor(string base58) => TransactorPublicKeyBase58Check.Equals(base58, StringComparison.CurrentCultureIgnoreCase);
        public bool IsNotTransactor(string base58) => !IsTransactor(base58);


        public ulong TxnIndexInBlock { get; set; }
        public string TxnType { get; set; }
        
        public List<AffectedPublicKeys> AffectedPublicKeys { get; set; }
        public BasicTransferTxIndexMetadata BasicTransferTxindexMetadata { get; set; } 
        public BitcoinExchangeTxIndexMetadata BitcoinExchangeTxindexMetadata { get; set; }
        public CreatorCoinTxIndexMetadata CreatorCoinTxindexMetadata { get; set; }
        public List< object > CreatorCoinTransferTxindexMetadata { get; set; }
        public UpdateProfileTxIndexMetadata UpdateProfileTxindexMetadata { get; set; }
        public SubmitPostTxIndexMetadata SubmitPostTxindexMetadata { get; set; }
        public LikeTxIndexMetadata LikeTxindexMetadata { get; set; }
        public FollowTxIndexMetadata FollowTxindexMetadata { get; set; }
        public PrivateMessageTxIndexMetadata PrivateMessageTxindexMetadata { get; set; }
        public List< object > SwapIdentityTxindexMetadata { get; set; }
    }
}