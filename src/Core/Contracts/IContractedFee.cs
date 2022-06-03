namespace CloutCast.Contracts
{
    public interface IContractedFee
    {
        IBitCloutUser Payee { get; }
        int Percentage { get; }
    }
}