using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(015), JetBrains.Annotations.UsedImplicitly]
    public class Migration015_Alter_EntityLog_View : Migration
    {
        public override void Up()
        {
            Execute.Sql($"DROP VIEW [dbo].[{Views.EntityLog}]");
            Execute.Sql($@"CREATE VIEW [dbo].[{Views.EntityLog}] AS
SELECT el.Id, el.TimeStamp, d.Name, el.Action, el.Active, el.UserId, el.PromotionId
FROM {Tables.EntityLog} el
INNER JOIN {Tables.Description} d on d.Value = Action and d.Type = 'EntityAction'");
        }

        public override void Down()
        {
            Execute.Sql($"DROP VIEW [dbo].[{Views.EntityLog}]");
            Execute.Sql($@"CREATE VIEW [dbo].[{Views.EntityLog}] AS
SELECT Id, TimeStamp, 
CASE
  WHEN Action = 0   THEN 'UnDefined'
  WHEN Action = 1   THEN 'User-AddFunds'
  WHEN Action = 2   THEN 'User-WithdrawFunds'
  WHEN Action = 10  THEN 'User-Register'
  WHEN Action = 15  THEN 'User-DidPromotion'
  WHEN Action = 17  THEN 'System-Fee'
  WHEN Action = 20  THEN 'Promotion-Start'
  WHEN Action = 25  THEN 'Promotion-Extend'
  WHEN Action = 30  THEN 'Promotion-Expire'
  WHEN Action = 35  THEN 'Promotion-Stop'
  WHEN Action = 100 THEN 'Founder-Reward'
END as Description, Action, Active, UserId, PromotionId
FROM {Tables.EntityLog}");
        }
    }
}