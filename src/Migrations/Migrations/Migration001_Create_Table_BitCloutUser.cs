using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(001), JetBrains.Annotations.UsedImplicitly]
    public class Migration001_Create_Table_BitCloutUser : Migration
    {
        private readonly string _userIdIndex = Tables.User.ToUniqueIndexName("UserId");

        public override void Up()
        {
            Create
                .Table(Tables.User)
                .WithColumn("Id").AsInt64().Unique(_userIdIndex).Identity()
                .WithColumn("PublicKey").AsString(58).NotNullable().PrimaryKey()
                .WithColumn("Handle").AsString(255).NotNullable();

            Execute.Sql($@"insert into {Tables.User} (PublicKey, Handle) values ('BC1YLiVetFBCYjuHZY5MPwBSY7oTrzpy18kCdUnTjuMrdx9A22xf5DE', 'System')");
        }

        public override void Down()
        {
            Delete.Index(_userIdIndex).OnTable(Tables.User);
            Delete.Table(Tables.User);
        }
    }
}
