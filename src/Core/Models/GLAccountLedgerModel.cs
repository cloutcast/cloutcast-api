using JetBrains.Annotations;

namespace CloutCast.Models
{
    [UsedImplicitly]
    public class GLAccountLedgerModel: GLAccountOwnerModel
    {
        public GeneralLedgerType Ledger { get; set; }
        public string BitCloutIdentifier { get; set; }

        public override string Describe()
        {
            var desc = base.Describe();
            if (desc.IsEmpty() || Ledger == GeneralLedgerType.Undefined) return desc;
            return $"To {Ledger} {desc}";
        }
    }
}