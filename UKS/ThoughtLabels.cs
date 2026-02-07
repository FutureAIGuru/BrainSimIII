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
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Collections.Concurrent;

namespace UKS;

public class ThoughtLabels
{
    static ConcurrentDictionary<string, Thought> labelList = new ConcurrentDictionary<string, Thought>();

    public static ConcurrentDictionary<string, Thought> LabelList { get => labelList;}

    public static Thought GetThought(string label)
    {
        if (label is null || label == "") return null;
        Thought retVal = null;
        if (labelList.TryGetValue(label.ToLower(), out retVal)) 
        { }  //breakpoint?
        return retVal;
    }
    public static string RemoveTrailingDigits(string s)
    {
        int i = s.Length;
        while (i > 0 && char.IsDigit(s[i - 1]))
            i--;

        return s[..i];
    }

    public static string AddThoughtLabel(string newLabel, Thought t)
    {
        //sets a label and appends/increments trailing digits in the event of collisions
        if (newLabel == "") return newLabel; //don't index empty lables
        labelList.TryRemove(t.Label.ToLower(), out Thought dummy);
        int curDigits = -1;
        string baseString = newLabel;
        //This code allows you to put a * at the end of a label and it will auto-increment
        if (newLabel.EndsWith("*"))
        {
            curDigits = 0;
            baseString = newLabel.Substring(0, newLabel.Length - 1);
            baseString = RemoveTrailingDigits(baseString);
            newLabel = baseString + curDigits;
        }

        //autoincrement in the event of name collisions
        while (!labelList.TryAdd(newLabel.ToLower(), t))
        {
            curDigits++;
            newLabel = baseString + curDigits;
        }
        return newLabel;
    }
    public static void ClearLabelList()
    {
        labelList.Clear();
    }
    public static List<Thought> AllThoughtsInLabelList()
    {
        List<Thought> retVal = new();
        foreach (Thought thought in labelList.Values) { retVal.Add(thought); }
        return retVal;
    }
    public static void RemoveThoughtLabel(string existingLabel)
    {
        if (string.IsNullOrEmpty(existingLabel)) return;
        labelList.Remove(existingLabel.ToLower(), out Thought oldThought);
    }

}
