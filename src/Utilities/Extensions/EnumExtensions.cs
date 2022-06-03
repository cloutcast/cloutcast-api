using System;
using System.ComponentModel;
using System.Linq;

namespace CloutCast
{
    public static class EnumExtensions
    {
        public static T[] All<T>(this T any) where T : Enum => Enum.GetValues(typeof(T)).Cast<T>().ToArray();

        public static bool IsIn<T>(this T source, params T[] statuses) where T : Enum, IConvertible => statuses.Any(s => s.Equals(source));

        public static string ToDescription<T>(this T enumValue) where T: Enum
        {
            var fi = enumValue.GetType().GetField(enumValue.ToString());

            if (null == fi) return enumValue.ToString();
            var attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
            return attrs.Length > 0 ? ((DescriptionAttribute)attrs[0]).Description : enumValue.ToString();
        }
    }
}