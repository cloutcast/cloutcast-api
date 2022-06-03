using System;
using System.Collections.Generic;

namespace CloutCast.Contracts
{
    public interface IBitCloutPost
    {
        string Body { get; }
        IEnumerable<IBitCloutPost> GetComments();
        IBitCloutCount Counts { get; }
        ulong DiamondsFromSender { get; }

        List<string> ImageURLs { get; }
        bool IsHidden { get; }
        bool IsPinned { get; }

        string PostHashHex { get; }
        string PosterPublicKeyBase58Check { get; }

        IBitCloutPost ReCloutedPostEntry { get; }

        DateTimeOffset TimeStamp { get; }
        long TimestampNanos { get; }
    }
}