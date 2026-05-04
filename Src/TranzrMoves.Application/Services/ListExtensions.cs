// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TranzrMoves.Application.Services;

public static class ListExtensions
{
    public static int FindIndex<T>(this IReadOnlyList<T> source, Predicate<T> predicate)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (predicate(source[i]))
                return i;
        }

        return -1;
    }
}
