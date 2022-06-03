using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(011), JetBrains.Annotations.UsedImplicitly]
    public class Migration011_Create_EntityLog_View : Migration
    {
        public override void Up() => Execute.Sql($@"
CREATE VIEW [dbo].[{Views.EntityLog}]
AS
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

        public override void Down() => Execute.Sql($"DROP VIEW [dbo].[{Views.EntityLog}]");
    }
}