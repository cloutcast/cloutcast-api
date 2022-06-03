namespace CloutCast.Models
{
    public class SingleUserProfile
    {
        public Profile Profile { get; set; }
        public bool IsBlacklisted { get; set; }
        public bool IsGraylisted { get; set; }
    }
}