using System;

namespace Utils
{
    public static class EnumUtils
    {
        public static TEnum GetEnumFromObject<TEnum>(object obj) where TEnum : struct, Enum
        {
            return obj switch
            {
                string str when Enum.TryParse<TEnum>(str, out var result) => result,
                int intVal when Enum.IsDefined(typeof(TEnum), intVal) => (TEnum)(intVal as object),
                _ => throw new ArgumentException($"Invalid object type or value for enum {typeof(TEnum).Name}")
            };
        }
    }
}