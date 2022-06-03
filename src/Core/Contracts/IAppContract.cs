using System.Collections.Generic;

namespace CloutCast.Contracts
{
    public interface IAppContract
    {
        EntityAction Action { get; }
        IAppSource App { get; }
        IEnumerable<IContractedFee> Fees { get; }
    }
}