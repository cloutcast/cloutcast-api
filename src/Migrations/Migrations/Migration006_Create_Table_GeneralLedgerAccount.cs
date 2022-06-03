using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(006), JetBrains.Annotations.UsedImplicitly]
    public class Migration006_Create_Table_GeneralLedgerAccount : Migration
    {
        private readonly string _promotionIdIndex;
        private readonly string _userIdIndex;
        private readonly string _uniqueCompoundIndex;
        
        public Migration006_Create_Table_GeneralLedgerAccount()
        {
            _promotionIdIndex = Tables.GeneralLedgerAccount.ToIndexName("PromotionId");
            _userIdIndex = Tables.GeneralLedgerAccount.ToIndexName("UserId");
            _uniqueCompoundIndex = Tables.GeneralLedgerAccount.ToUniqueIndexName("LedgerType", "PromotionId", "UserId");
        }
        public override void Up()
        {
            Create
                .Table(Tables.GeneralLedgerAccount)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("LedgerTypeId").AsByte().NotNullable()
                .WithColumn("PromotionId").AsInt64().Nullable().Indexed(_promotionIdIndex)
                .WithColumn("UserId").AsInt64().Nullable().Indexed(_userIdIndex);

            Create
                .Index(_uniqueCompoundIndex)
                .OnTable(Tables.GeneralLedgerAccount)
                .OnColumn("LedgerTypeId").Ascending()
                .OnColumn("PromotionId").Ascending()
                .OnColumn("UserId").Unique();

            this
                .CreateForeignKey(Tables.GeneralLedgerAccount, Tables.Promotion)
                .CreateForeignKey(Tables.GeneralLedgerAccount, Tables.User)
                .CreateForeignKey(Tables.GeneralLedgerAccount, Tables.GeneralLedgerType, "LedgerTypeId")
                .Execute.Sql($@"
insert into {Tables.GeneralLedgerAccount} (LedgerTypeId, PromotionId, UserId)
select glType.Id, null, u.Id
from {Tables.GeneralLedgerType} glType
cross Join {Tables.User} u");
        }

        public override void Down()
        {
            this
                .DeleteForeignKey(Tables.GeneralLedgerAccount, Tables.Promotion)
                .DeleteForeignKey(Tables.GeneralLedgerAccount, Tables.User)
                .DeleteForeignKey(Tables.GeneralLedgerAccount, Tables.GeneralLedgerType, "LedgerTypeId");


            Delete.Index(_uniqueCompoundIndex).OnTable(Tables.GeneralLedgerAccount);
            Delete.Index(_promotionIdIndex).OnTable(Tables.GeneralLedgerAccount);
            Delete.Index(_userIdIndex).OnTable(Tables.GeneralLedgerAccount);
            Delete.Table(Tables.GeneralLedgerAccount);
        }
    }
}