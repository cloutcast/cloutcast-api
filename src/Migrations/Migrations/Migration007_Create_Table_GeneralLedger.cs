using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(007), JetBrains.Annotations.UsedImplicitly]
    public class Migration007_Create_Table_GeneralLedger : Migration
    {
        private readonly string _debitAccountIndex;
        private readonly string _creditAccountIndex;
        private readonly string _entityLogIndex;

        public Migration007_Create_Table_GeneralLedger()
        {
            _creditAccountIndex = Tables.GeneralLedger.ToIndexName("CreditAccountId");
            _debitAccountIndex = Tables.GeneralLedger.ToIndexName("DebitAccountId");
            _entityLogIndex = Tables.GeneralLedger.ToIndexName("EntityLogId");
        }

        public override void Up()
        {
            Create
                .Table(Tables.GeneralLedger)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Amount").AsInt64().NotNullable()
                .WithColumn("EntityLogId").AsInt64().NotNullable().Indexed(_entityLogIndex)
                .WithColumn("DebitAccountId").AsInt64().NotNullable().Indexed(_debitAccountIndex)
                .WithColumn("CreditAccountId").AsInt64().NotNullable().Indexed(_creditAccountIndex)
                .WithColumn("Memo").AsString(1024).Nullable();

            this
                .CreateForeignKey(Tables.GeneralLedger, Tables.GeneralLedgerAccount, "CreditAccountId")
                .CreateForeignKey(Tables.GeneralLedger, Tables.GeneralLedgerAccount, "DebitAccountId")
                .CreateForeignKey(Tables.GeneralLedger, Tables.EntityLog);
        }

        public override void Down()
        {
            this
                .DeleteForeignKey(Tables.GeneralLedger, Tables.EntityLog)
                .DeleteForeignKey(Tables.GeneralLedger, Tables.GeneralLedgerAccount, "DebitAccountId")
                .DeleteForeignKey(Tables.GeneralLedger, Tables.GeneralLedgerAccount, "CreditAccountId");

            Delete.Index(_entityLogIndex).OnTable(Tables.GeneralLedger);
            Delete.Index(_debitAccountIndex).OnTable(Tables.GeneralLedger);
            Delete.Index(_creditAccountIndex).OnTable(Tables.GeneralLedger);
            Delete.Table(Tables.GeneralLedger);
        }
    }
}