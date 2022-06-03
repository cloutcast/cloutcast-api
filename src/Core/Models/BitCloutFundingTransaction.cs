namespace CloutCast.Models
{
    using Contracts;

    public abstract class BitCloutFundingTransaction : IBitCloutFundingTransaction
    {
        public long Amount { get; set; }
        public string EvidencePostHex { get; set; }
        public string UserPublicKey { get; set; }

        public abstract bool IsInput();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((BitCloutFundingTransaction) obj);
        }

        protected bool Equals(BitCloutFundingTransaction other)
            => Amount == other.Amount &&
               EvidencePostHex?.Trim() == other.EvidencePostHex?.Trim() &&
               UserPublicKey?.Trim() == other.UserPublicKey?.Trim();

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ (EvidencePostHex != null ? EvidencePostHex.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (UserPublicKey != null ? UserPublicKey.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(BitCloutFundingTransaction left, BitCloutFundingTransaction right) => Equals(left, right);
        public static bool operator !=(BitCloutFundingTransaction left, BitCloutFundingTransaction right) => !Equals(left, right);
    }
}