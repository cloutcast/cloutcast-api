using System.ComponentModel;

namespace CloutCast
{
    public enum UserRole
    {
        Creator = 1,
        Promoter = 2,
        Client = 4
    }

    public enum ProfileSetting
    {
        [Description("Allow Others To Promote")] AllowOthersToPromote = 10,
        [Description("Minimun Acceptable Inbox Payment")] MinInboxPayment = 11,
        [Description("Promoter Ration")] PromoterRatio = 12
    }
}