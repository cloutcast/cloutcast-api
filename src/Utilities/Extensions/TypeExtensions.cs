using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CloutCast
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> BaseTypesAndSelf(this Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        public static Type GetListType(this Type type) => (type.GetInterfaces()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>))
            .Select(t => t.GetGenericArguments()[0])).FirstOrDefault();

        public static object GetDefault(this Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        public static IEnumerable<PropertyInfo> GetAllPublicProperties(this Type type)
        {
            //https://stackoverflow.com/questions/358835/getproperties-to-return-all-properties-for-an-interface-inheritance-hierarchy

            if (!type.IsInterface)
                return type.GetProperties();

            return (new[] {type})
                .Concat(type.GetInterfaces())
                .SelectMany(i => i.GetProperties())
                .Distinct();
        }

        public static IEnumerable<MethodInfo> GetMethodsOfReturnType(this Type cls, Type ret)
        {
            // Did you really mean to prohibit public methods? I assume not
            var methods = cls.GetMethods(BindingFlags.NonPublic | 
                                         BindingFlags.Public |
                                         BindingFlags.Instance);
            return methods.Where(m => m.ReturnType.IsAssignableFrom(ret));
        }

        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy 
                        | BindingFlags.Public 
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
        }

        public static bool IsNullable<T>(this T obj)
        {
            if (obj == null) return true; // obvious
            var type = typeof(T);
            if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }

        public static bool IsNumericType(this object o)
        {   
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
        
        public static string GetName<T>(this Expression<Func<T>> action) => GetNameFromMemberExpression(action.Body);

        static string GetNameFromMemberExpression(Expression expression) {
            if (expression is MemberExpression memberExpression) 
                return memberExpression.Member.Name;
            
            if (expression is UnaryExpression unaryExpression) 
                return GetNameFromMemberExpression(unaryExpression.Operand);
            
            return "MemberNameUnknown";
        }
    }
}