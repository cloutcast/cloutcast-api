namespace CloutCast.Models
{
    using Contracts;

    public class GLAccountOwnerModel : IGeneralLedgerAccountOwner
    {
        public GLAccountOwnerModel() { }

        public GLAccountOwnerModel(long id, GeneralLedgerAccountType type)
        {
            Id = id;
            Type = type;
        }
        public GLAccountOwnerModel(IGeneralLedgerAccountOwner source)
        {
            if (source == null) return;
            Id = source.Id;
            Type = source.Type;
        }

        public long Id { get; set; }
        public GeneralLedgerAccountType Type { get; set; }
        
        public bool IsPromotion() => Type == GeneralLedgerAccountType.Promotion;
        public bool IsUser() => Type == GeneralLedgerAccountType.User;

        public virtual string Describe()
        {
            switch (Type)
            {
                case GeneralLedgerAccountType.User: return $"UserId[{Id}]";
                case GeneralLedgerAccountType.Promotion: return $"PromotionId[{Id}]";
                default: return "";
            }
        }
        public override string ToString() => Describe();
    }
}