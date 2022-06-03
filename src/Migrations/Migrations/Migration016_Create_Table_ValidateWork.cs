using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(016), JetBrains.Annotations.UsedImplicitly]
    public class Migration016_Create_Table_ValidateWork : Migration
    {
        private readonly string _glIndex;
        public Migration016_Create_Table_ValidateWork() => 
            _glIndex = Tables.ValidateWork.ToIndexName("EntityLogId");

        public override void Up()
        {
            Create
                .Table(Tables.ValidateWork)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityLogId").AsInt64().Unique(_glIndex) //pow
                .WithColumn("CheckOn").AsDateTimeOffset().NotNullable()
                .WithColumn("Result").AsBoolean().Nullable();

            Execute.Sql($@"
insert into {Tables.ValidateWork} (EntityLogId, CheckOn, Result)
select el.Id, DATEADD(d,2, el.TimeStamp), 1
from {Tables.Evidence} e
inner join {Tables.GeneralLedger} gl on gl.Id = e.GeneralLedgerId
inner join {Tables.EntityLog} el on el.Id = gl.EntityLogId
where el.Action = {(int)EntityAction.UserDidPromotion}");

            Execute.Sql($@"
declare @Lookup table(ExpiredId bigint, PayoutId bigint, GlId bigint)

--Add Payout rows to EntityLog
merge {Tables.EntityLog} target
using (
	select el.TimeStamp, el.Active, el.UserId, el.PromotionId, el.Id as ExpiredId, gl.Id as GLId
	from {Tables.GeneralLedger} gl
	inner join {Tables.EntityLog} el on el.Id = gl.EntityLogId and el.Action = 30
	inner join (
		select 
		  gl.Amount, 
		  el.PromotionId,
		  promoterPayable.Id as payableId, 
		  promoterDeposit.Id as depositId
		from {Tables.GeneralLedger} gl
		inner join {Tables.EntityLog} el on el.Id = gl.EntityLogId and el.Action = 15
		inner join {Tables.GeneralLedgerAccount} promotionPayable on promotionPayable.Id = gl.DebitAccountId and promotionPayable.PromotionId = el.PromotionId --Promo.Payable
		inner join {Tables.GeneralLedgerAccount} promoterPayable on promoterPayable.Id = gl.CreditAccountId and promoterPayable.LedgerTypeId = 20   --Promoter.Payable
		inner join {Tables.GeneralLedgerAccount} promoterDeposit on (promoterDeposit.UserId = promoterPayable.UserId and promoterDeposit.LedgerTypeId = 30) -- Promoter.Deposit
	)  x on 
	  x.Amount = gl.Amount and
	  x.payableId = gl.DebitAccountId and
	  x.depositId = gl.CreditAccountId
	where el.PromotionId = x.PromotionId
) src ON (1=0)
WHEN NOT MATCHED THEN INSERT ( TimeStamp, Action, Active, UserId, PromotionId )
  VALUES (src.TimeStamp, 41, src.Active, src.UserId, src.PromotionId)
OUTPUT src.ExpiredId, Inserted.Id as PayoutId, src.GLId
INTO @Lookup;

--Move Payout GL entries to Payout EntityLog entries
update gl
set gl.EntityLogId = l.PayoutId
from {Tables.GeneralLedger} gl
inner join @Lookup l on l.GLId = gl.Id");
        }

        public override void Down()
        {
            Delete.Index(_glIndex).OnTable(Tables.ValidateWork);
            Delete.Table(Tables.ValidateWork);
        }
    }
}