using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloutCast
{
    public static class AssemblyExtensions
    {
        public static void ForEachWithAttribute<T>(this Assembly assembly, Action<T,Type> work) where T : Attribute
        {
            if (work == null) return;
            foreach (var type in assembly.GetTypes())
            {
                foreach (var customAttribute in type.GetCustomAttributes(typeof(T), true).OfType<T>())
                {
                    work.Invoke(customAttribute, type);
                }
            }
        }

        public static IEnumerable<T> AllWithAttribute<T>(this Assembly assembly) where T : Attribute =>
            assembly.GetTypes()
                .SelectMany(type => type.GetCustomAttributes(typeof(T), true))
                .Cast<T>();
    }
}