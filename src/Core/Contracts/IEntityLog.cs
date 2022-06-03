using System;

namespace CloutCast.Contracts
{
    public interface IEntityLog
    {
        EntityAction Action { get; }
        DateTimeOffset TimeStamp { get; }
        IBitCloutUser User { get; }
    }
}