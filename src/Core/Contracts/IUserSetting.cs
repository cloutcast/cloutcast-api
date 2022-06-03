namespace CloutCast.Contracts
{
    public interface IUserProfile
    {
        bool AllowOthersToPromote();
        ulong MinimumAcceptablePayment();
        decimal PromoterRatio();
    }
    public interface IUserSetting
    {
        public UserRole Role { get; }
        public ProfileSetting Setting { get; }
        public object Value { get; }
    }
}