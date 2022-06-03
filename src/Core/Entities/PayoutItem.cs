namespace SkunkWerx.CloutCast.Entities
{
    using System;
    using Contracts;

    public class PayoutItem: IEntity
    {
        public long Id { get; set; }        //GeneralLedger.Id
        public DateTimeOffset TimeStamp { get; set; }   //EntityLog.Timestamp
        public string Name { get; set; }    //EntityLog.Name
        public long Amount { get; set; }    //GeneralLedger.Amount
        public PayoutActorItem Debitor { get; set; }
        public PayoutActorItem Creditor { get; set; }
    }
    
    public class PayoutActorItem: IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string Ledger { get; set; }
    }

}