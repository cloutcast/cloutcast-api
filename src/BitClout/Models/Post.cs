namespace CloutCast.Models
{
    using Contracts;

    [JetBrains.Annotations.UsedImplicitly]
    public class Post: PostEntry
    {
        public Post RecloutedPostEntryResponse { get; set; }
        public bool InGlobalFeed { get; set; }

        public override IBitCloutPost ReCloutedPostEntry => RecloutedPostEntryResponse;
    }
}