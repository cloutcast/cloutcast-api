using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(012), JetBrains.Annotations.UsedImplicitly]
    public class Migration012_AddColumn_SystemFee_To_Table_Promotion : Migration
    {
        public override void Up()
        {
            Alter.Table(Tables.Promotion).AddColumn("Engagements").AsInt64().NotNullable().SetExistingRowsTo(0);
            Alter.Table(Tables.Promotion).AddColumn("SystemFee").AsInt64().NotNullable().SetExistingRowsTo(0);
            
            Execute.Sql($@"
update promo
set 
   Engagements = iif(engagement.count > SystemFee.ActualEngagements, engagement.Count, SystemFee.ActualEngagements), 
   SystemFee = SystemFee.AverageFee
from {Tables.Promotion} promo
inner join
(
	select x.PromotionId, NoFeeBudget / (Rate* 1000000) as Count
	from (
		select p.Id as PromotionId, p.Rate, Cast( ( (p.Budget / 1.04 ) * 1000000) as  bigint) as NoFeeBudget		
		from {Tables.Promotion} p
	) x
) engagement on engagement.PromotionId = promo.Id
inner join (
	select el.PromotionId, Avg(gl.Amount) as AverageFee, count(*) as ActualEngagements
	from {Tables.GeneralLedger} gl
	inner join {Tables.EntityLog} el on el.Id = gl.EntityLogId
	where el.Action = {(int)EntityAction.SystemFee}
	group by el.PromotionId
) as SystemFee on SystemFee.PromotionId = promo.Id");

            Delete.Column("Budget").FromTable(Tables.Promotion);
        }

        public override void Down()
        {
            Alter.Table(Tables.Promotion).AddColumn("Budget").AsInt64().NotNullable().SetExistingRowsTo(0);

            Execute.Sql($"update p set Budget = (p.Rate + p.SystemFee) * p.Engagements from {Tables.Promotion} p");

            Delete
                .Column("SystemFee")
                .Column("Engagements")
                .FromTable(Tables.Promotion);
        }
    }
}