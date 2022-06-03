using System;
using System.Text;
using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(014), JetBrains.Annotations.UsedImplicitly]
    public class Migration014_Create_Table_Description : Migration
    {
        private static void AddDescription<T>(StringBuilder sb, T val) where T : Enum
        {
            sb.AppendLine($"\t('{val}', '{val.GetType().Name}', {Convert.ChangeType(val, TypeCode.Int32)}),");
        }
        
        public override void Up()
        {
            Create
                .Table(Tables.Description)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Name").AsString(255).NotNullable()
                .WithColumn("Type").AsString(100).NotNullable()
                .WithColumn("Value").AsInt32().NotNullable();

            var sb = new StringBuilder()
                .AppendLine($"insert into {Tables.Description} (Name, Type, Value)")
                .AppendLine("values ");

            foreach (var val in EntityAction.UnDefined.All()) AddDescription(sb, val);
            foreach (var val in GeneralLedgerAccountType.Undefined.All()) AddDescription(sb, val);
            foreach (var val in GeneralLedgerAction.Undefined.All()) AddDescription(sb, val);
            foreach (var val in GeneralLedgerType.Undefined.All()) AddDescription(sb, val);
            foreach (var val in PromotionActivity.Undefined.All()) AddDescription(sb, val);

            sb.Length -= 3;

            Execute.Sql(sb.ToString());
        }

        public override void Down() => Delete.Table(Tables.Description);
    }
}