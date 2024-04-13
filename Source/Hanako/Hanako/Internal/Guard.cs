using System;

namespace Hanako.Internal;

internal static class Guard
{
    public static T IsNotNull<T>(T argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        return argument;
    }

    public static void HasLength<T>(Memory<T> collection, uint length)
    {
        if (collection.Length != length)
        {
            throw new ArgumentOutOfRangeException(nameof(collection));
        }
    }

    public static void HasLengthBiggerOrEqual<T>(Memory<T> collection, uint length)
    {
        if (collection.Length >= length)
        {
            throw new ArgumentOutOfRangeException(nameof(collection));
        }
    }
}
