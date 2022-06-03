namespace CloutCast.Contracts
{
    public interface IBitCloutFundingTransaction
    {
        long Amount { get;  }
        string UserPublicKey { get; }
        string EvidencePostHex { get;  }

        bool IsInput();
    }
}