using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(009), JetBrains.Annotations.UsedImplicitly]
    public class Migration009_Create_User_TableType : Migration
    {
        public override void Up()
        {
            this.CreateUserTableType(Tables.UserTableType, @"
[Id] [bigint] NULL,
[PublicKey] [varchar](58) NULL");
        }

        public override void Down() => this.DropUserTableType(Tables.UserTableType);
    }
}