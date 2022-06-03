using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(003), JetBrains.Annotations.UsedImplicitly]
    public class Migration003_Create_Table_Promotion : Migration
    {
        private readonly string _promotionUsersType;
        private readonly string _userIdIndex;
        private readonly string _targetKey = Tables.Promotion.ToIndexName("TargetKey");
        private readonly string _promotionRefCol = Tables.Promotion.ToReferenceCol();
        private readonly string _promotionIdIndex;

        public Migration003_Create_Table_Promotion()
        {
            _userIdIndex = Tables.Promotion.ToIndexName(Tables.User.ToReferenceCol());
            _promotionIdIndex = Tables.PromotionUsers.ToIndexName(_promotionRefCol);
            _promotionUsersType = $"UT_{Tables.PromotionUsers}";
        }

        public override void Up()
        {
            Create
                .Table(Tables.Promotion)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn(Tables.User.ToReferenceCol()).AsInt64().NotNullable().Indexed(_userIdIndex)
                .WithColumn("Budget").AsInt64().NotNullable()
                .WithColumn("Duration").AsInt32().NotNullable() // In minutes
                .WithColumn("MinCoinPrice").AsInt64().Nullable()
                .WithColumn("MinFollowerCount").AsInt16().Nullable()
                .WithColumn("BitCloutToUsdRate").AsDecimal(28, 14).NotNullable()
                .WithColumn("Rate").AsInt64().NotNullable()
                .WithColumn("TargetAction").AsByte().NotNullable()
                .WithColumn("TargetHex").AsString(64).NotNullable().Indexed(_targetKey);
            
            Create
                .Table(Tables.PromotionUsers)
                .WithColumn(_promotionRefCol).AsInt64().NotNullable().Indexed(_promotionIdIndex)
                .WithColumn("PublicKey").AsString(58).NotNullable();

            this.CreateUserTableType(_promotionUsersType, $@"
	[{_promotionRefCol}] [bigint] NOT NULL,
	[PublicKey] [varchar](58) NOT NULL");

            this.CreateForeignKey(Tables.PromotionUsers, Tables.Promotion);
            this.CreateForeignKey(Tables.Promotion, Tables.User);
        }

        public override void Down()
        {
            this
                .DropUserTableType(_promotionUsersType)
                .DeleteForeignKey(Tables.Promotion, Tables.User)
                .DeleteForeignKey(Tables.PromotionUsers, Tables.Promotion);

            Delete.Index(_promotionIdIndex).OnTable(Tables.PromotionUsers);
            Delete.Table(Tables.PromotionUsers);

            Delete.Index(_targetKey).OnTable(Tables.Promotion);
            Delete.Index(_userIdIndex).OnTable(Tables.Promotion);
            Delete.Table(Tables.Promotion);
        }
    }
}