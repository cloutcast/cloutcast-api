using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(023), JetBrains.Annotations.UsedImplicitly]
    public class Migration023_Create_Table_UserProfile : Migration
    {
        private readonly string _userIdCol;
        private readonly string _userIdIndex;

        public Migration023_Create_Table_UserProfile()
        {
            _userIdCol = Tables.User.ToReferenceCol();
            _userIdIndex = Tables.UserProfile.ToIndexName(_userIdCol);
        }

/*
    UserId | Role      | Setting                | Value           | Description                                                                                    Action               
    -------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------
    111    | Creator   | AllowOthersToPromote   | [TRUE | FALSE]  | As a creator do I want other users to be able to promote my content [default : TRUE]         | CreatePromotion  
    111    | Promoter  | MinInboxPayout         | $NANOs          | The min payout amount a user will accept from @inbox [default 0]                             | CreatePromotion
    111    | Client    | PromoterRatio          | up to 1.0       | Percentage of the total amount of my Promotions that any one Promoter can do [default 50%]   | ProofOfWork
 */

        public override void Up()
        {
            Create
                .Table(Tables.UserProfile)
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn(_userIdCol).AsInt64().NotNullable().Indexed(_userIdIndex)
                .WithColumn("Role").AsByte().NotNullable()
                .WithColumn("Setting").AsString(100).NotNullable()
                .WithColumn("Value").AsString(1024).NotNullable();

            this.CreateForeignKey(Tables.UserProfile, Tables.User, _userIdCol);
        }

        public override void Down()
        {
            this.DeleteForeignKey(Tables.UserProfile, Tables.User, _userIdCol);
            Delete.Table(Tables.UserProfile);
        }
    }
}