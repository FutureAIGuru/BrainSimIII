//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System.Collections.Generic;
using System.Collections.Concurrent;

namespace BrainSimulator.Modules
{
    public partial class Thing
    {
        //This hack is needed because add-parent/add-child rely on the has-child relationship which may not exist yet
        private static Thing hasChildType;
        public static Thing HasChild
        {
            get
            {
                if (hasChildType == null)
                {
                    hasChildType = ThingLabels.GetThing("has-child");
                    if (hasChildType == null)
                    {
                        Thing thingRoot = ThingLabels.GetThing("Thing");
                        if (thingRoot == null) return null;
                        Thing relTypeRoot = ThingLabels.GetThing("RelationshipType");
                        if (relTypeRoot == null)
                        {
                            hasChildType = new Thing() { Label = "has-child" };
                            relTypeRoot = new Thing() { Label = "RelationshipType" };
                            thingRoot.AddRelationship(relTypeRoot, hasChildType);
                            relTypeRoot.AddRelationship(hasChildType, hasChildType);
                        }
                    }
                }
                return hasChildType;
            }
            set { hasChildType = value; }
        }
    }

    public class ThingLabels
    {
        static ConcurrentDictionary<string, Thing> labelList = new ConcurrentDictionary<string, Thing>();
        public static Thing GetThing(string label)
        {
            if (label == null || label == "") return null;
            Thing retVal = null;
            if (labelList.TryGetValue(label.ToLower(), out retVal)) { }
            return retVal;
        }
        public static string AddThingLabel(string newLabel, Thing t)
        {
            //sets a label and appends/increments trailing digits in the event of collisions
            if (newLabel == "") return newLabel; //don't index empty lables
            labelList.TryRemove(t.Label.ToLower(), out Thing dummy);
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
            Thing.HasChild = null;
        }
        public static List<Thing> AllThingsInLabelList()
        {
            List<Thing> retVal = new();
            foreach (Thing thing in labelList.Values) { retVal.Add(thing); }
            return retVal;
        }
        public static void RemoveThingLabel(string existingLabel)
        {
            if (existingLabel == "") return;
            labelList.Remove(existingLabel,out Thing oldThing);
        }

    }




    //this is a modification of Thing which is used to store and retrieve the KB in XML
    //it eliminates circular references by replacing Thing references with int indexed into an array and makes things much more compact
    public class SThing
    {
        public string label = ""; //this is just for convenience in debugging and should not be used
        public List<SRelationship> relationships = new();
        object value;
        public object V { get => value; set => this.value = value; }
        public int useCount;
    }

}
