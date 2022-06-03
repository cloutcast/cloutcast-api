using FluentMigrator;

namespace SkunkWerx.CloutCast.Migrations
{
    [Migration(026), JetBrains.Annotations.UsedImplicitly]
    public class Migration026_Alter_PromotionUser_Add_ReadReceipt : Migration
    {
        public override void Up() => Alter
                .Table(Tables.PromotionUsers)
                .AddColumn("ReadOn").AsDateTimeOffset().Nullable();

        public override void Down() => Delete.Column("ReadOn").FromTable(Tables.PromotionUsers);
    }
}