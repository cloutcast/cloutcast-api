using System.Collections.Generic;

namespace CloutCast.Models
{
    using Contracts;

    public class AppContractModel : IAppContract
    {
        public EntityAction Action { get; set; }
        public IAppSource App { get; set; }
        public List<ContractedFeeModel> Fees { get; set; }

        IEnumerable<IContractedFee> IAppContract.Fees => Fees;
    }
}