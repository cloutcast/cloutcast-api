namespace CloutCast.Records
{
    using Contracts;
    using Models;

    internal class UserSettingRecord : UserSetting<string>
    {
        public long UserId { get; set; }

        public IUserSetting ToSetting()
        {
            switch (Setting)
            {
                case ProfileSetting.AllowOthersToPromote:
                    return new UserToggleSettingModel
                    {
                        Role = Role,
                        Setting = Setting,
                        Value = !bool.TryParse(Value, out var boolVal) || boolVal
                    };
                    
                case ProfileSetting.MinInboxPayment:
                    return new UserNanoSettingModel
                    {
                        Role = Role,
                        Setting = Setting,
                        Value = ulong.TryParse(Value, out var nanoVal) ? nanoVal : 0
                    };

                case ProfileSetting.PromoterRatio:
                    return new UserRatioSettingModel
                    {
                        Role = Role,
                        Setting = Setting,
                        Value = decimal.TryParse(Value, out var temp) ? temp : 0
                    };
            }

            return null;
        }
    }
}