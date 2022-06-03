namespace CloutCast.Models
{
    using Contracts;

    [JetBrains.Annotations.UsedImplicitly]
    public class Comment: PostEntry
    {
        public override IBitCloutPost ReCloutedPostEntry => null;
    }
}