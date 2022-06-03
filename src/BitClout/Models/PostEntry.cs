using System;
using System.Collections.Generic;

namespace CloutCast.Models
{
    using Contracts;

    public abstract class PostEntry : IBitCloutPost
    {
        public List<Comment> Comments { get; set; }
        public ulong ConfirmationBlockHeight { get; set; }
        public ulong CreatorBasisPoints { get; set; }
        public bool InMempool { get; set; }
        public List<object> ParentPosts { get; set; }

        public string ParentStakeID { get; set; }
        public ulong StakeMultipleBasisPoints { get; set; }
        public StakeEntry StakeEntry { get; set; }
        public StakeEntryStats StakeEntryStats { get; set; }

        public ProfileEntryResponse ProfileEntryResponse { get; set; }
        public PostEntryReaderState PostEntryReaderState { get; set; }
        public PostExtraData PostExtraData { get; set; }
        
        #region IBitCloutPost
        private readonly BitCloutCounts _count = new BitCloutCounts();
        public string Body { get; set; }
        public ulong DiamondsFromSender { get; set; }
        
        public List<string> ImageURLs { get; set; }
        public bool IsHidden { get; set; }
        public bool IsPinned { get; set; }

        public string PostHashHex { get; set; }
        public string PosterPublicKeyBase58Check { get; set; }

        public abstract IBitCloutPost ReCloutedPostEntry { get; }

        public DateTimeOffset TimeStamp => DateTimeOffset.FromUnixTimeMilliseconds(TimestampNanos / 1000000);
        public long TimestampNanos { get; set; }
        #endregion

        #region IBitCloutCount
        public ulong CommentCount
        {
            get => _count.Comment;
            set => _count.Comment = value;
        }
        public ulong DiamondCount
        {
            get => _count.Diamond;
            set => _count.Diamond = value;
        }
        public ulong LikeCount
        {
            get => _count.Like;
            set => _count.Like = value;
        }
        public ulong RecloutCount
        {
            get => _count.Reclout;
            set => _count.Reclout = value;
        }

        public IEnumerable<IBitCloutPost> GetComments() => Comments ?? new List<Comment>();

        IBitCloutCount IBitCloutPost.Counts => _count;

        class BitCloutCounts : IBitCloutCount
        {
            public ulong Like { get; set; }
            public ulong Diamond { get; set; }
            public ulong Comment { get; set; }
            public ulong Reclout { get; set; }
        }
        #endregion
    }
}