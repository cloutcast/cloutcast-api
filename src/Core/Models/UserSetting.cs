namespace CloutCast.Models
{
    using Contracts;

    public abstract class UserSetting<T>: IUserSetting 
    {
        public UserRole Role { get; set; }
        public ProfileSetting Setting { get; set; }
        public T Value { get; set; }
        object IUserSetting.Value => Value;
    }

    public class UserToggleSettingModel : UserSetting<bool> { }
    public class UserNanoSettingModel : UserSetting<ulong> { }
    public class UserRatioSettingModel : UserSetting<decimal> { }
}