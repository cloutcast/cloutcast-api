namespace CloutCast.Models
{
    public class BitCloutIncomingFunds : BitCloutFundingTransaction
    {
        public override bool IsInput() => true;
    }
}