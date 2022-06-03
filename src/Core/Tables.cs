namespace CloutCast
{
    public static class Tables
    {
        public static readonly TableName App = new TableName("App");
        public static readonly TableName Contract = new TableName("AppContract");

        public static readonly TableName Description = new TableName("Description");
        public static readonly TableName EntityLog = new TableName("EntityLog");
        public static readonly TableName Evidence = new TableName("Evidence");

        public static readonly TableName GeneralLedger = new TableName("GeneralLedger");
        public static readonly TableName GeneralLedgerAccount = new TableName("GeneralLedgerAccount");
        public static readonly TableName GeneralLedgerType = new TableName("GeneralLedgerType");

        public static readonly TableName Promotion = new TableName("Promotion");
        public static readonly TableName PromotionUsers = new TableName("PromotionUsers");
        
        public static readonly TableName User = new TableName("BitCloutUser");
        public static readonly TableName UserProfile = new TableName("UserProfile");
        public static readonly TableName UserVerification = new TableName("UserVerification");

        public static readonly TableName ValidateWork = new TableName("ValidateWork");
        
        public static string UserTableType => $"UT_{User}";
    }

    public static class Views
    {
        public static string EntityLog => "EntityLogView";
        public static string GLAccount => "GLAccountView";
    }
}