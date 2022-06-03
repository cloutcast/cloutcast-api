using System.Collections.Generic;

namespace CloutCast.Contracts
{
    public interface IPromotionCriteria
    {
        long MinCoinPrice { get; }
        int MinFollowerCount { get; }
        List<string> AllowedUsers { get; }
    }
}