using System.Collections.Generic;

namespace CloutCast.Models
{
    [JetBrains.Annotations.UsedImplicitly]
    public class Profile
    {
        public string PublicKeyBase58Check { get; set; }
        public string Username { get; set; }
        public string Description { get; set; }
        public string ProfilePic { get; set; }
        public bool IsHidden { get; set; }
        public bool IsReserved { get; set; }
        public bool IsVerified { get; set; }
        public List<Comment> Comments { get; set; }
        public List<PostEntry> Posts { get; set; }
        public CoinEntry CoinEntry { get; set; }
        public ulong CoinPriceBitCloutNanos { get; set; }
        public ulong StakeMultipleBasisPoints { get; set; }
        public StakeEntryStats StakeEntryStats { get; set; }
        public object UsersThatHODL { get; set; }
    }
}