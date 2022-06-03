namespace CloutCast.Models
{
    using Contracts;

    public class ContractedFeeModel : IContractedFee
    {
        public IBitCloutUser Payee { get; set; }
        public int Percentage { get; set; }
    }
}