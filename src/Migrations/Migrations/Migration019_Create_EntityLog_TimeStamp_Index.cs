using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(019), JetBrains.Annotations.UsedImplicitly]
    public class Migration019_Create_EntityLog_TimeStamp_Index : Migration
    {
        public override void Up() => Create
            .Index(Tables.EntityLog.ToIndexName("TimeStamp", "Action", "PromotionId"))
            .OnTable(Tables.EntityLog).WithOptions().NonClustered()
            .OnColumn("TimeStamp").Ascending()
            .OnColumn("Action").Ascending()
            .OnColumn("PromotionId");

        public override void Down() => Delete
            .Index(Tables.EntityLog.ToIndexName("TimeStamp", "Action", "PromotionId"))
            .OnTable(Tables.EntityLog);
    }
}