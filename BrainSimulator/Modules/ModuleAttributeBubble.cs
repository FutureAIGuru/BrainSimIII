//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml.Serialization;
using UKS;
using static System.Math;

namespace BrainSimulator.Modules;

public class ModuleAttributeBubble : ModuleBase
{
    // Fill this method in with code which will execute
    // once for each cycle of the engine
    public override void Fire()
    {
        //This agent works on a timer and "Fire" is not used

        Init();

        UpdateDialog();
    }

    public bool isEnabled { get; set; }

    private Timer timer;
    //private UKS.UKS theUKS1;
    public string debugString = "Initialized\n";
    private void Setup()
    {
        if (timer is null)
        {
            timer = new Timer(SameThreadCallback, null, 0, 10000);
        }
    }
    private void SameThreadCallback(object state)
    {
        new Thread(() =>
        {
            DoTheWork();
        }).Start();
    }

    private class RelDest
    {
        public Thing relType;
        public Thing target;
        public List<Relationship> relationships = new();
        public override string ToString()
        {
            return $"{relType.Label} -> {target.Label}  :  {relationships.Count}";
        }
    }

    public void DoTheWork()
    {
        debugString = "Bubbler Started\n";
        foreach (Thing t in theUKS.UKSList)
        {
            if (t.Children.Count == 0) continue;
            if (!t.HasAncestor("Object")) continue;
            if (t.Label == "unknownObject") continue;

            //build a List of all the Relationships which this thing's children have
            List<RelDest> itemCounts = new();
            foreach (Thing t1 in t.Children)
            {
                foreach (Relationship r in t1.Relationships)
                {
                    if (r.reltype == Thing.HasChild) continue;
                    Thing useRelType = GetInstanceType(r.reltype);

                    RelDest foundItem = itemCounts.FindFirst(x => x.relType == useRelType && x.target == r.target);
                    if (foundItem == null)
                    {
                        foundItem = new RelDest { relType = useRelType, target = r.target };
                        itemCounts.Add(foundItem);
                    }
                    foundItem.relationships.Add(r);
                }
            }
            if (itemCounts.Count == 0) continue;
            var sortedItems = itemCounts.OrderByDescending(x => x.relationships.Count).ToList();

            List<string> excludeTypes = new List<string>() { "hasProperty", "isTransitive", "isCommutative", "inverseOf", "hasAttribute", "hasDigit" };
            //bubble the relationships
            for (int i = 0; i < sortedItems.Count; i++)
            {
                RelDest rr = sortedItems[i];
                if (excludeTypes.Contains(rr.relType.Label, comparer: StringComparer.OrdinalIgnoreCase)) continue;
                //if (rr.relationships.Count < t.Children.Count / 2) break;

                //find an existing relationship
                Relationship r = theUKS.GetRelationship(t, rr.relType, rr.target);
                float currentWeight = (r != null) ? r.Weight : 0;

                int positiveCount = rr.relationships.FindAll(x => x.Weight > .5f).Count;
                int negativeCount = 0;
                //are there any conflicting relationships
                for (int j = 0; j < sortedItems.Count; j++)
                {
                    if (j == i) continue;
                    if (RelationshipsConflict(rr, sortedItems[j]))
                        negativeCount += sortedItems[j].relationships.Count;
                }
                int noInfoCount = t.Children.Count - (positiveCount + negativeCount); //not currently used 
                if (negativeCount >= positiveCount)
                {
                    if (r != null)
                    {
                        t.RemoveRelationship(r);
                        debugString += $"Removed {r.ToString()} \n";
                    }
                    continue;
                }

                //calculate what the new/added weight should be
                float newWeight = rr.relationships.Average(x => x.Weight);
                if (newWeight != currentWeight)
                {
                    if (newWeight < .5)
                    {
                        if (r != null)
                        {
                            t.RemoveRelationship(r);
                            debugString += $"Removed {r.ToString()} \n";
                        }
                    }
                    else
                    {
                        //bubble the property
                        r = t.AddRelationship(rr.target, rr.relType);
                        r.Weight = newWeight;
                        r.Fire();
                        debugString += $"{r.ToString()}   {r.Weight.ToString(".0")} \n";
                    }
                }
            }
        }
        debugString += "Bubbler Finished\n";
        UpdateDialog();
    }

    //We need 1) count for this Relationship, 2) count for any conflicting, 3) count without a reference
    //If some relationships are exceptions, we can still bubble the 
    //Relationships are exceptions if they conflict AND numbers are one are small relative to the other.
    //a conflicting Relationship is:
    //  reltypes are the same AND targets are different but have a common parent w/ isexclusive (colors)
    //  targets are the same AND relTypes are different and have attributes with acommon parent which has the IsExslucive property (counts) (have 3, have 4)
    // Modified from UKS.CS line 181.  This does not includ AllowMultiples as these should not be bubbled
    private bool RelationshipsConflict(RelDest r1, RelDest r2)
    {
        if (r1.relType == r2.relType && r1.target == r2.target) return false;
        if (r1.relType == r2.relType)
        {
            var parents = FindCommonParents(r1.target, r2.target);
            foreach (var parent in parents)
                if (parent.HasPropertyLabeled("isExclusive")) return true;
        }
        if (r1.target == r2.target)
        {
            var parents = FindCommonParents(r1.target, r2.target);
            foreach (var parent in parents)
                if (parent.HasPropertyLabeled("isExclusive")) return true;

            //get the attributes of the relationships
            IList<Thing> r1RelAttribs = theUKS.GetAttributes(r1.relType);
            IList<Thing> r2RelAttribs = theUKS.GetAttributes(r2.relType);

            Thing r1Not = r1RelAttribs.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thing r2Not = r2RelAttribs.FindFirst(x => x.Label == "not" || x.Label == "no");
            if (r1Not == null && r2Not != null || r1Not != null && r2Not == null)
                return true;

            //are any of the attrbutes which are exclusive?
            foreach (Thing t1 in r1RelAttribs)
                foreach (Thing t2 in r2RelAttribs)
                {
                    if (t1 == t2) continue;
                    List<Thing> commonParents = FindCommonParents(t1, t2);
                    foreach (Thing t3 in commonParents)
                    {
                        if (t3.HasPropertyLabeled("isexclusive") || t3.HasPropertyLabeled("allowMultiple"))
                            return true;
                    }
                }
            // handle special case where one reltype has is numberic and the other is not
            bool hasNumber1 = (r1RelAttribs.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            bool hasNumber2 = (r2RelAttribs.FindFirst(x => x.HasAncestorLabeled("number")) != null);
            if (hasNumber1 || hasNumber2) return true;

        }
        return false;
    }


    private static List<Thing> FindCommonParents(Thing t, Thing t1)
    {
        //BORROWED from UKSStatement.cs line 323
        List<Thing> commonParents = new List<Thing>();
        foreach (Thing p in t.Parents)
            if (t1.Parents.Contains(p))
                commonParents.Add(p);
        return commonParents;
    }


    bool BubbleNeeded()
    {
        return true;
    }





    //if the given thing is an instance of its parent, get the parent
    private static Thing GetInstanceType(Thing t)
    {
        Thing useRelType = t;
        while (useRelType.Parents.Count > 0 && EndsInInteger(useRelType.Label) && !t.Label.Contains(".") && useRelType.Label.StartsWith(useRelType.Parents[0].Label))
            useRelType = useRelType.Parents[0];
        return useRelType;
    }

    public static bool EndsInInteger(string input)
    {
        // Regular expression to check if the string ends with a sequence of digits
        return Regex.IsMatch(input, @"\d+$");
    }
    // Fill this method in with code which will execute once
    // when the module is added, when "initialize" is selected from the context menu,
    // or when the engine restart button is pressed
    public override void Initialize()
    {
        Setup();
    }

    // The following can be used to massage public data to be different in the xml file
    // delete if not needed
    public override void SetUpBeforeSave()
    {
    }
    public override void SetUpAfterLoad()
    {
        Setup();
    }

    // called whenever the UKS performs an Initialize()
    public override void UKSInitializedNotification()
    {

    }
}