using System;

namespace CloutCast.Contracts
{
    public interface IPromotionTarget
    {
        PromotionActivity Action { get; }
        DateTimeOffset? CreationDate { get; }
        string Hex { get; }
    }
}