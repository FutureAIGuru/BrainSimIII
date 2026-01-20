//
// Copyright (c) FutureAI. All rights reserved.  
// Contains confidential and  proprietary information and programs which may not be distributed without a separate license
//  

namespace UKS;

//these are used so that relationship lists can be readOnly.
//This prevents programmers from accidentally doing a (e.g.) relationships.Add() which will not handle reverse links properly
//Why the IReadOnlyList doesn't have FindFirst, FindAll, and FindIndex is a ???

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

