using System.Collections.Generic;
using System.Linq;

namespace CloutCast.Queries
{
    using Models;

    public class GetExistingIncomeQuery : DapperQuery<List<BitCloutIncomingFunds>>
    {
        public override void Build(IStatementBuilder builder) => builder.Add($@"
select gl.Amount, TRIM(e.PostHex) as EvidencePostHex, TRIM(bu.PublicKey) as UserPublicKey
from {Tables.GeneralLedger} gl
inner join {Tables.GeneralLedgerAccount} da on da.Id = gl.DebitAccountId and da.LedgerTypeId = {(int) GeneralLedgerType.Cash} 
inner join {Tables.GeneralLedgerAccount} ca on ca.Id = gl.CreditAccountId and ca.LedgerTypeId = {(int) GeneralLedgerType.Deposit}
inner join {Tables.Evidence} e on e.EntityLogId = gl.EntityLogId
inner join {Tables.User} bu on bu.Id = da.UserId ");

        public override List<BitCloutIncomingFunds> Read(IDapperGridReader reader) => reader.Read<BitCloutIncomingFunds>().ToList();
    }
}