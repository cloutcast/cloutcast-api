using System;
using System.Linq;
using JetBrains.Annotations;

namespace CloutCast.Queries
{
    using Commands;
    using Models;
    using Records;
    
    public interface IGetBalanceForUserQuery : IDapperQuery<AccountBalanceModel>
    {
        IGetBalanceForUserQuery AsOf(DateTimeOffset asOf);
        IGetBalanceForUserQuery UserId(long userId);
    }

    [UsedImplicitly]
    public class GetBalanceForUserQuery : DapperQuery<AccountBalanceModel>, IGetBalanceForUserQuery
    {
        private readonly IFetchBalanceForUserCommand _fetchBalances;

        public GetBalanceForUserQuery(IFetchBalanceForUserCommand fetchBalances) => _fetchBalances = fetchBalances;

        public override void Build(IStatementBuilder builder)
        {
            _fetchBalances
                .AsOf(_asOf)
                .AccountOwner(GeneralLedgerAccountType.User, _userId)
                .TotalFor(GeneralLedgerType.Deposit, "TotalUserDeposit")
                .TotalFor(GeneralLedgerType.Payable, "TotalUserPayable")
                .Build(builder);
            
            builder.Append($@"
select @TotalUserDeposit as Total, {(int)GeneralLedgerType.Deposit} as Ledger
union
select @TotalUserPayable as Total, {(int)GeneralLedgerType.Payable} as Ledger");
        }

        public override AccountBalanceModel Read(IDapperGridReader reader)
        {
            var records = reader.Read<TotalByLedgerRecord>().ToList();

            return new AccountBalanceModel
            {
                AccountOwner = new GLAccountOwnerModel
                {
                    Id = _userId,
                    Type = GeneralLedgerAccountType.User
                },
                AsOf = _asOf,
                Settled = records.SingleOrDefault(r => r.Ledger == GeneralLedgerType.Deposit)?.Total ?? 0,
                UnSettled = records.SingleOrDefault(r => r.Ledger == GeneralLedgerType.Payable)?.Total ?? 0
            };
        }
      
        #region IGetUserBalancesQuery
        private DateTimeOffset _asOf = DateTimeOffset.UtcNow;
        public IGetBalanceForUserQuery AsOf(DateTimeOffset asOf) => this.Fluent(x => _asOf = asOf);

        private long _userId;
        public IGetBalanceForUserQuery UserId(long userId) => this.Fluent(x => _userId = userId);
        #endregion
    }
}