using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(013), JetBrains.Annotations.UsedImplicitly]
    public class Migration013_Remove_Table_UserVerification : Migration
    {
        private readonly string _keyIndex = Tables.UserVerification.ToIndexName("PublicKey");

        public override void Up() 
        {
            Delete.Index(_keyIndex ).OnTable(Tables.UserVerification);
            Delete.Table(Tables.UserVerification);
        }

        public override void Down() => Create
            .Table(Tables.UserVerification)
            .WithColumn("UniqueId").AsString(50).NotNullable().PrimaryKey()
            .WithColumn("BitCloutHandle").AsString(255).NotNullable()
            .WithColumn("PublicKey").AsString(80).NotNullable().Indexed(_keyIndex)
            .WithColumn("CreatedOn").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("PassPhrase").AsString(1024).NotNullable();
    }
}