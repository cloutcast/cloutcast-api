using System.Collections.Generic;
using System.Linq;

namespace SkunkWerx.CloutCast.Queries
{
    using Entities;
    using PureLogicTek;

    public interface IGetPayoutListQuery : IDapperQuery<List<PayoutItem>> { }
    public class GetPayoutListQuery : DapperQuery<List<PayoutItem>>, IGetPayoutListQuery
    {
        public override void Build(IStatementBuilder builder) => builder.Add($@"
 select 
    gl.Id, 
    elv.TimeStamp, 
    elv.Name, 
    gl.Amount,
    da.AccountOwnerId as Id, da.AccountOwner as Name, da.AccountOwnerPublicKey as PublicKey, concat('DEBIT ', UPPER(da.LedgerType)) as Ledger,
    ca.AccountOwnerId as Id, ca.AccountOwner as Name, ca.AccountOwnerPublicKey as PublicKey, concat('CREDIT ', UPPER(ca.LedgerType)) as Ledger    
 from {Tables.GeneralLedger} gl 
 inner join {Views.EntityLog} elv on elv.Id = gl.EntityLogId
 inner join {Views.GLAccount} ca on ca.LedgerAccountId = gl.CreditAccountId
 inner join {Views.GLAccount} da on da.LedgerAccountId = gl.DebitAccountId
 left outer join {Tables.Evidence} e on e.EntityLogId = elv.Id
 where elv.Action = 2
 and e.PostHex is null
 order by gl.Id desc
");
        
        public override List<PayoutItem> Read(IDapperGridReader reader) => reader
                .Map<PayoutItem, PayoutActorItem, PayoutActorItem>(Map)
                .ToList();

        private static void Map(PayoutItem payout, PayoutActorItem debitor, PayoutActorItem creditor)
        {
            if (payout == null) return;
            payout.Debitor = debitor;
            payout.Creditor = creditor;
        }
    }
}