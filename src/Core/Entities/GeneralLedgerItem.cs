namespace CloutCast.Entities
{
    using Contracts;
    using Models;

    public class GeneralLedgerItem : IEntity
    {
        public long Id { get; set; }
        public long Amount { get; set; }
        public string EvidencePostHex { get; set; }
        public string Memo { get; set; }

        public GLAccountLedgerModel Credit { get; set; }
        public GLAccountLedgerModel Debit { get; set; }
        public EntityLog EntityLog { get; set; }
    }
}