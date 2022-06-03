using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(008), JetBrains.Annotations.UsedImplicitly]
    public class Migration008_Create_Table_Evidence: Migration
    {
        private readonly string _uniqueIndex;
        public Migration008_Create_Table_Evidence() => 
            _uniqueIndex = Tables.Evidence.ToIndexName("GeneralLedgerId");

        public override void Up()
        {
            Create
                .Table(Tables.Evidence)
                .WithColumn("GeneralLedgerId").AsInt64().PrimaryKey().Unique(_uniqueIndex)
                .WithColumn("PostHex").AsCustom("nchar(64)").NotNullable();

            this.CreateForeignKey(Tables.Evidence, Tables.GeneralLedger, "GeneralLedgerId");
        }

        public override void Down()
        {
            this.DeleteForeignKey(Tables.Evidence, Tables.GeneralLedger);
            Delete.Index(_uniqueIndex).OnTable(Tables.Evidence);
            Delete.Table(Tables.Evidence);
        }
    }
}