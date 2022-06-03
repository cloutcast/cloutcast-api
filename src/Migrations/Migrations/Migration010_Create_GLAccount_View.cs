using FluentMigrator;

namespace CloutCast.Migrations
{
    [Migration(010), JetBrains.Annotations.UsedImplicitly]
    public class Migration010_Create_GLAccount_View : Migration
    {
        public override void Up() => Execute.Sql(@"
CREATE VIEW [dbo].[GLAccountView]
AS
SELECT 
  gla.Id AS LedgerAccountId, 
  CASE 
    WHEN gla.UserId IS NULL THEN 'Promotion' 
    WHEN p.Id IS NULL THEN 'User' 
  END AS AccountType, 
  ISNULL(gla.UserId, p.Id) AS AccountOwnerId, 
  CASE 
    WHEN gla.UserId IS NULL THEN CONCAT('Promotion ', p.Id)
    WHEN p.Id IS NULL THEN ISNULL(u.Handle, CONCAT('User', u.Id))
  END AS AccountOwner, 
  glt.Name AS LedgerType

FROM  dbo.GeneralLedgerAccount AS gla 
INNER JOIN dbo.GeneralLedgerType AS glt ON gla.LedgerTypeId = glt.Id 
LEFT OUTER JOIN dbo.Promotion AS p ON p.Id = gla.PromotionId
LEFT OUTER JOIN BitCloutUser u on u.Id = gla.UserId AND u.Handle NOT Like '%UnVerified -%'
");

        public override void Down() => Execute.Sql("DROP VIEW [dbo].[GLAccountView]");
    }
}