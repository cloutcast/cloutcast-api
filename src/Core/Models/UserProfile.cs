using System.Collections.Generic;
using System.Linq;

namespace CloutCast.Models
{
    using Contracts;

    public class UserProfile : IUserProfile
    {
        private readonly Dictionary<ProfileSetting, IUserSetting> _settings;
        public UserProfile() => _settings = new Dictionary<ProfileSetting, IUserSetting>();
        public UserProfile(IEnumerable<IUserSetting> settings)
        {
            _settings = settings != null 
                ? settings?.ToDictionary(s => s.Setting, s => s) 
                : new Dictionary<ProfileSetting, IUserSetting>();
        }

        public bool AllowOthersToPromote()
        {
            if (!_settings.ContainsKey(ProfileSetting.AllowOthersToPromote))
                return true;

            var setting = (UserToggleSettingModel) _settings[ProfileSetting.AllowOthersToPromote];
            return setting.Value;
        }

        public ulong MinimumAcceptablePayment()
        {
            if (!_settings.ContainsKey(ProfileSetting.MinInboxPayment))
                return 0;

            var setting = (UserNanoSettingModel) _settings[ProfileSetting.MinInboxPayment];
            return setting.Value;
        }

        public decimal PromoterRatio()
        {
            if (!_settings.ContainsKey(ProfileSetting.PromoterRatio))
                return 0.6m;

            var setting = (UserRatioSettingModel) _settings[ProfileSetting.PromoterRatio];
            return setting.Value < 0 ? 0.6m : setting.Value;
        }
    }
}