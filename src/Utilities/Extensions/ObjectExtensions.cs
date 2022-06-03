using System;

namespace CloutCast
{
    public static class ObjectExtensions
    {
        public static T Fluent<T>(this T source, Action<T> action) where T : class
        {
            action.Invoke(source);
            return source;
        }
    }
}