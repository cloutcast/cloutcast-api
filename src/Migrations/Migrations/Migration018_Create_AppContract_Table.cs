using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(018), JetBrains.Annotations.UsedImplicitly]
    public class Migration018_Create_AppContract_Table : Migration
    {
        private readonly string _appId = Tables.App.ToReferenceCol();
        private readonly string _appIdIndex;
        private readonly string _userId = Tables.User.ToReferenceCol();
        private readonly string _userIdIndex;

        public Migration018_Create_AppContract_Table()
        {
            _appIdIndex = Tables.Contract.ToIndexName(_appId);
            _userIdIndex = Tables.Contract.ToIndexName(_userId);
        }

        public override void Up()
        {
            Create
                .Table(Tables.Contract)
                .WithColumn(_appId).AsInt64().NotNullable().Indexed(_appIdIndex)
                .WithColumn(_userId).AsInt64().NotNullable().Indexed(_userIdIndex)
                .WithColumn("Action").AsInt16().NotNullable()
                .WithColumn("Percentage").AsInt16().NotNullable();

            this
                .AddConstraint(Tables.Contract, "(Percentage > (0) AND Percentage <= (100))")
                .CreateForeignKey(Tables.Contract, Tables.App)
                .CreateForeignKey(Tables.Contract, Tables.User)
                .Execute.Sql($@"
insert into {Tables.Contract} (AppId, UserId, Action, Percentage)
select ap.Id, u.Id, {(int)EntityAction.UserDidPromotion}, 100
from {Tables.App} ap
cross join {Tables.User} u
where ap.Name = 'CloutCast'
and u.PublicKey = 'BC1YLiVetFBCYjuHZY5MPwBSY7oTrzpy18kCdUnTjuMrdx9A22xf5DE'");
        }

        public override void Down()
        {
            this
                .DeleteConstraint(Tables.Contract)
                .DeleteForeignKey(Tables.Contract, Tables.App)
                .DeleteForeignKey(Tables.Contract, Tables.User);

            Delete.Index(_appIdIndex).OnTable(Tables.Contract);
            Delete.Table(Tables.Contract);
        }
    }
}