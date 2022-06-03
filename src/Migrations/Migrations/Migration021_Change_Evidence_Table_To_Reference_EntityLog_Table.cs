using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(021), JetBrains.Annotations.UsedImplicitly]
    public class Migration021_Alter_Evidence_Table_To_Reference_EntityLog_Table : Migration
    {
        const string GLId = "GeneralLedgerId";
        private readonly TableName _old;
        private readonly string _entityLogId = Tables.EntityLog.ToReferenceCol();
        private readonly string _entityLogIdIndex;
        private readonly string _uniqueIndex = Tables.Evidence.ToIndexName(GLId);

        public Migration021_Alter_Evidence_Table_To_Reference_EntityLog_Table()
        {
            _old = new TableName("OldEvidence");
            _entityLogIdIndex = Tables.Evidence.ToIndexName(_entityLogId);
        }

        public override void Up()
        {
            this.DeleteForeignKey(Tables.Evidence, Tables.GeneralLedger);
            Execute.Sql($"ALTER TABLE {Tables.Evidence} DROP CONSTRAINT [PK_Evidence]");
            Rename.Table(Tables.Evidence).To(_old);

            Create
                .Table(Tables.Evidence)
                .WithColumn(_entityLogId).AsInt64().PrimaryKey().Unique(_uniqueIndex)
                .WithColumn("PostHex").AsCustom("nchar(64)").NotNullable();

            this.CreateForeignKey(Tables.Evidence, Tables.EntityLog, _entityLogId);

            Execute.Sql($@"
insert into {Tables.Evidence} ({_entityLogId}, PostHex)
select gl.EntityLogId, old.PostHex
from {_old} old
inner join {Tables.GeneralLedger} gl on gl.Id = old.GeneralLedgerId");

            Delete.Table(_old);
        }

        public override void Down()
        {
            this.DeleteForeignKey(Tables.Evidence, Tables.EntityLog);
            Execute.Sql($"ALTER TABLE {Tables.Evidence} DROP CONSTRAINT [PK_Evidence]");
            Rename.Table(Tables.Evidence).To(_old);

            Create
                .Table(Tables.Evidence)
                .WithColumn(GLId).AsInt64().PrimaryKey().Unique(_uniqueIndex)
                .WithColumn("PostHex").AsCustom("nchar(64)").NotNullable();

            this.CreateForeignKey(Tables.Evidence, Tables.GeneralLedger, GLId);

            Execute.Sql($@"
insert into {Tables.Evidence} ({GLId}, PostHex)
select gl.Id, old.PostHex
from {_old} old 
inner join {Tables.EntityLog} el on el.Id = old.{_entityLogId}
inner join {Tables.GeneralLedger} gl on gl.EntityLogId = el.Id
");

            Delete.Table(_old);
        }
    }
}