using System.Linq;
using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(005), JetBrains.Annotations.UsedImplicitly]
    public class Migration005_Create_Table_LedgerType : Migration
    {
        public override void Up()
        {
            Create
                .Table(Tables.GeneralLedgerType)
                .WithColumn("Id").AsByte().PrimaryKey()
                .WithColumn("Name").AsString(255).NotNullable();

            foreach (var glType in GeneralLedgerType.Undefined.All().Where(e => e != GeneralLedgerType.Undefined))
                Execute.Sql($"insert into {Tables.GeneralLedgerType} (Id, Name) values ({(byte)glType}, '{glType.ToDescription()}')");
        }

        public override void Down() => Delete.Table(Tables.GeneralLedgerType);
    }
}