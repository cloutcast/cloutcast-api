using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(022), JetBrains.Annotations.UsedImplicitly]
    public class Migration022_AddColumn_TargetCreationDate_To_Table_Promotion : Migration
    {
        public override void Up() => Alter.Table(Tables.Promotion).AddColumn("TargetCreationDate").AsDateTimeOffset().Nullable();
        public override void Down() => Delete.Column("TargetCreationDate").FromTable(Tables.Promotion);
    }
}