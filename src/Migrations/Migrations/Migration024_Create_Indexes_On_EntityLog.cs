using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(024), JetBrains.Annotations.UsedImplicitly]
    public class Migration024_Create_Indexes_On_EntityLog : Migration
    {
        private readonly TableName _mainTable = Tables.EntityLog;
        private readonly string _index;
        public Migration024_Create_Indexes_On_EntityLog() => 
            _index = _mainTable.ToIndexName("Action", "PromotionId", "UserId");

        public override void Up() => this
            .CreateIndexIfNotExists(_mainTable, _index, c => c
                .OnColumn("Action").Ascending()
                .OnColumn("PromotionId").Ascending()
                .OnColumn("UserId").Ascending()
            );

        public override void Down() => Delete.Index(_index).OnTable(_mainTable);
    }
}