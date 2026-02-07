/*
 * Brain Simulator Thought
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Thought and is licensed under
 * the MIT License. You may use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of this software under the terms of
 * the MIT License.
 *
 * See the LICENSE file in the project root for full license information.
 */
//
// From the Future AI Society and Charles Simon
// Available for use under an MIT license.
//  

namespace UKS;

//these are used so that lists can be readOnly and still be searched propertly.
//This prevents us from accidentally doing a (e.g.) _links.Add() which will not handle reverse links properly
//Why the IReadOnlyList doesn't have FindFirst, FindAll, Contains, and FindIndex is a mystery.

public static class IReadOnlyListExtensions
{
    public static T? FindFirst<T>(this IReadOnlyList<T> source, Func<T, bool> condition)
    {
        foreach (T item in source)
            if (condition(item))
                return item;
        return default(T);
    }
    public static IReadOnlyList<T> FindAll<T>(this IReadOnlyList<T> source, Func<T, bool> condition)
    {
        List<T> theList = new List<T>();
        if (source is null) return theList;
        foreach (T item in source)
            if (condition(item))
                theList.Add(item);
        return theList;
    }
    public static int FindIndex<T>(this IReadOnlyList<T> source, Func<T, bool> condition)
    {
        for (int i = 0; i < source.Count; i++)
        {
            T item = source[i];
            if (condition(item))
                return i;
        }
        return -1;
    }

    public static bool Contains<T>(this IReadOnlyList<T> list, T value)
    {
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < list.Count; i++)
        {
            if (comparer.Equals(list[i], value))
                return true;
        }
        return false;
    }
}

