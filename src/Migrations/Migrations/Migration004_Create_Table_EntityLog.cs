using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(004), JetBrains.Annotations.UsedImplicitly]
    public class Migration004_Create_Table_EntityLog : Migration
    {
        private readonly TableName _mainTable = Tables.EntityLog;

        private readonly string _promotionId = Tables.Promotion.ToReferenceCol();
        private readonly string _promotionIdIndex;

        private readonly string _userId = Tables.User.ToReferenceCol();
        private readonly string _userIdIndex;

        public Migration004_Create_Table_EntityLog()
        {
            _promotionIdIndex = _mainTable.ToIndexName(_promotionId);
            _userIdIndex = _mainTable.ToIndexName(_userId);
        }

        public override void Up() => Create
            .Table(_mainTable)
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("TimeStamp").AsDateTimeOffset().NotNullable()
            .WithColumn("Action").AsInt16().NotNullable()
            .WithColumn("Active").AsInt16().NotNullable()
            .WithColumn(_userId).AsInt64().NotNullable().Indexed(_userIdIndex)
            .WithColumn(_promotionId).AsInt64().Nullable().Indexed(_promotionIdIndex);

        public override void Down()
        {
            Delete.Index(_userIdIndex).OnTable(_mainTable);
            Delete.Index(_promotionIdIndex).OnTable(_mainTable);
            Delete.Table(_mainTable);
        }
    }
}