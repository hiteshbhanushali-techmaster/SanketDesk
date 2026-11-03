using System;
using System.Runtime.CompilerServices;

namespace Notifier
{
    /// <summary>
    /// Internal framework compatibility helpers between .NET Framework 4.6.1 and .NET 8.
    /// </summary>
    internal static class CompatibilityHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] EmptyArray<T>()
        {
            return Array.Empty<T>();
        }
    }
}
