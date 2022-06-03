namespace CloutCast.Contracts
{
    public interface IBitCloutCount
    {
        ulong Like { get; }
        ulong Diamond { get; }
        ulong Comment { get; }
        ulong Reclout { get; }
    }
}