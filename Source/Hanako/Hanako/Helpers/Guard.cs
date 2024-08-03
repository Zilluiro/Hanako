using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hanako.Helpers;

internal static class Guard
{
    [return: NotNull]
    public static T IsNotNull<T>(T argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        return argument;
    }

    public static void HasLength<T>(ReadOnlySequence<T> collection, uint length)
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
