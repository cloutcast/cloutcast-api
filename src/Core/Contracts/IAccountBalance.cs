using System;

namespace CloutCast.Contracts
{
    public interface IAccountBalance
    {
        IGeneralLedgerAccountOwner AccountOwner { get; }

        DateTimeOffset AsOf { get; }

        long Settled { get; }
        long UnSettled { get; }
    }
}