//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Collections.Concurrent;

namespace UKS;

public class CognemeLabels
{
    static ConcurrentDictionary<string, Cogneme> labelList = new ConcurrentDictionary<string, Cogneme>();

    public static ConcurrentDictionary<string, Cogneme> LabelList { get => labelList;}

    public static Cogneme GetThing(string label)
    {
        if (label is null || label == "") return null;
        Cogneme retVal = null;
        if (labelList.TryGetValue(label.ToLower(), out retVal)) 
        { }  //breakpoint?
        return retVal;
    }
    public static string AddThingLabel(string newLabel, Cogneme t)
    {
        //sets a label and appends/increments trailing digits in the event of collisions
        if (newLabel == "") return newLabel; //don't index empty lables
        labelList.TryRemove(t.Label.ToLower(), out Cogneme dummy);
        int curDigits = -1;
        string baseString = newLabel;
        //This code allows you to put a * at the end of a label and it will auto-increment
        if (newLabel.EndsWith("*"))
        {
            curDigits = 0;
            baseString = newLabel.Substring(0, newLabel.Length - 1);
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
    public static List<Cogneme> AllThingsInLabelList()
    {
        List<Cogneme> retVal = new();
        foreach (Cogneme thing in labelList.Values) { retVal.Add(thing); }
        return retVal;
    }
    public static void RemoveThingLabel(string existingLabel)
    {
        if (existingLabel == "") return;
        labelList.Remove(existingLabel.ToLower(), out Cogneme oldThing);
    }

}
