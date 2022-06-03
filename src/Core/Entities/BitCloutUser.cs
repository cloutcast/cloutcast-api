using CloutCast.Models;

namespace CloutCast.Entities
{
    using Contracts;

    public class BitCloutUser : AbstractEntity<BitCloutUser>, IBitCloutUser
    {
        public string Handle { get; set; }
        public bool BlackList { get; set; }

        public string PublicKey { get; set; } 

        public ulong? CoinPrice { get; set; }
        public long? FollowerCount { get; set; }
        
        public UserProfile Profile { get; set; }
        IUserProfile IBitCloutUser.Profile => Profile;

        public IBitCloutUser CopyFrom(IBitCloutUser source)
        {
            CoinPrice = source.CoinPrice;
            FollowerCount = source.FollowerCount;
            Handle = source.Handle;
            PublicKey = source.PublicKey;

            if (source is IEntity entity) Id = entity.Id;
            return this;
        }

        public override string ToString() => this.ToDescription();
    }
}