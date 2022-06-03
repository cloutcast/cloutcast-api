using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(017), JetBrains.Annotations.UsedImplicitly]
    public class Migration017_Create_App_Table : Migration
    {
        private string ApiKeyIndex => Tables.App.ToIndexName("ApiKey");
        private string AppIdIndex => Tables.EntityLog.ToIndexName("AppId");

        public override void Up()
        {
            Create
                .Table(Tables.App)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ApiKey").AsString(50).NotNullable().Indexed(ApiKeyIndex)
                .WithColumn("Company").AsString(255).Nullable()
                .WithColumn("Name").AsString(255).NotNullable();

            Alter
                .Table(Tables.EntityLog)
                .AddColumn("AppId").AsInt64().NotNullable().SetExistingRowsTo(0)
                .AlterColumn("Active").AsInt16().NotNullable().WithDefaultValue(0)
                .Indexed(AppIdIndex);

            Execute.Sql(@$"
declare @Applications as table (id bigint, Name varchar(255))

insert into {Tables.App} (ApiKey, Company, Name) 
output inserted.Id, inserted.Name into @Applications
values 
  ('69f801de-a4e2-489a-b6c2-8e051abec2d5',  null, 'System'),
  ('e186719d-ce93-4b12-baab-a823181f99d0', 'CloutCast', 'CloutCast'),
  ('44a3996a-7e99-4d0b-8b92-4e282ddfc9f3', null, 'CloutFeed')

update {Tables.EntityLog} 
set AppId = (select Id from @Applications where Name = 'System')
where Action  in (
    {(int)EntityAction.UserAddFunds}, 
    {(int)EntityAction.UserWithdrawFunds},
    {(int)EntityAction.UserWorkPayOut},
    {(int)EntityAction.UserWorkRefund}
)

update {Tables.EntityLog} 
set AppId = (select Id from @Applications where Name = 'CloutCast')
where AppId = 0");

            this.CreateForeignKey(Tables.EntityLog, Tables.App);
        }

        public override void Down()
        {
            this.DeleteForeignKey(Tables.EntityLog, Tables.App);

            Delete.Index(ApiKeyIndex).OnTable(Tables.App);
            Delete.Table(Tables.App);
        }
    }
}