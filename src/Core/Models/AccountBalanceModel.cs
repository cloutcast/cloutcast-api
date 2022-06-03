using System;

namespace CloutCast.Models
{
    using Contracts;
    
    public class AccountBalanceModel : IAccountBalance
    {
        public GLAccountOwnerModel AccountOwner { get; set; }
        IGeneralLedgerAccountOwner IAccountBalance.AccountOwner => AccountOwner;

        public DateTimeOffset AsOf { get; set; }
        public long Settled { get; set; }
        public long UnSettled { get; set; }
    }
}