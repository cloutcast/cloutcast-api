namespace CloutCast.Models
{
    public class BitCloutOutGoingFunds : BitCloutFundingTransaction
    {
        public override bool IsInput() => false;
    }
}