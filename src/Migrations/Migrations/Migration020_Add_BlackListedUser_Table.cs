using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(020), JetBrains.Annotations.UsedImplicitly]
    public class Migration020_Add_BlackList_To_User_Table : Migration
    {
        public override void Up() => Alter
            .Table(Tables.User)
            .AddColumn("BlackList").AsBoolean()
            .WithDefaultValue(false)
            .NotNullable()
            .SetExistingRowsTo(false);

        public override void Down() => Delete.Column("BlackList").FromTable(Tables.User);
    }
}