//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UKS;

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
        if (!isEnabled) return;
        new Thread(() =>
        {
            DoTheWork();
        }).Start();
    }

    public class RelDest
    {
        public Thing relType;
        public Thing target;
        public List<Relationship> relationships = new();
        public RelDest()
        { }
        public RelDest(Relationship r)
        {
            relType = r.relType;
            target = r.target;
            relationships.Add(r);
        }
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
            if (t.Label == "Animal")
            { }
            if (t.HasAncestor("Object"))
                BubbleChildAttributes(t);
        }
        debugString += "Bubbler Finished\n";
        UpdateDialog();
    }
    void BubbleChildAttributes(Thing t)
    {
        if (t.Children.Count == 0) return;
        if (t.Label == "unknownObject") return;

        //build a List of all the Relationships which this thing's children have
        List<RelDest> itemCounts = new();
        foreach (Thing t1 in t.ChildrenWithSubclasses)
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
        if (itemCounts.Count == 0) return;
        var sortedItems = itemCounts.OrderByDescending(x => x.relationships.Count).ToList();

        List<string> excludeTypes = new List<string>() { "hasProperty", "isTransitive", "isCommutative", "inverseOf", "hasAttribute", "hasDigit" };
        //bubble the relationships
        for (int i = 0; i < sortedItems.Count; i++)
        {
            RelDest rr = sortedItems[i];
            if (excludeTypes.Contains(rr.relType.Label, comparer: StringComparer.OrdinalIgnoreCase)) continue;

            //find an existing relationship
            Relationship r = theUKS.GetRelationship(t, rr.relType, rr.target);
            float currentWeight = (r != null) ? r.Weight : 0f;

            //We need 1) count for this Relationship, 2) count for any conflicting, 3) count without a reference
            float totalCount = t.Children.Count;
            float positiveCount = rr.relationships.FindAll(x => x.Weight > .5f).Count;
            float positiveWeight = rr.relationships.Sum(x => x.Weight);
            float negativeCount = 0;
            float negativeWeight = 0;
            //are there any conflicting relationships
            for (int j = 0; j < sortedItems.Count; j++)
            {
                if (j == i) continue;
                if (RelationshipsConflict(rr, sortedItems[j]))
                {
                    negativeCount += sortedItems[j].relationships.Count; //?  why not += 1
                    negativeWeight += sortedItems[j].relationships.Sum(x => x.Weight);
                }
            }
            float noInfoCount = totalCount - (positiveCount + negativeCount);
            positiveWeight += currentWeight + noInfoCount * 0.51f;
            if (noInfoCount < 0) noInfoCount = 0;

            if (negativeCount >= positiveCount)
            {
                if (r != null)
                {
                    t.RemoveRelationship(r);
                    debugString += $"Removed {r} \n";
                }
                continue;
            }


            //calculate the new weight
            //If there is an existing weight, it is increased/decreased by a small amound and removed if it drops below .5
            //If there is no existing weight, it is assumed to start at 0.5.
            //TODO, replace this hardcoded "lookup table" with a formula
            float targetWeight = 0;
            float deltaWeight = positiveWeight - negativeWeight;
            if (deltaWeight < .8) targetWeight = -.1f;
            else if (deltaWeight < 1.7) targetWeight = .01f;
            else if (deltaWeight < 2.7) targetWeight = .2f;
            else targetWeight = .3f;
            if (currentWeight == 0) currentWeight = 0.5f;
            float newWeight = currentWeight + targetWeight;
            if (newWeight > 0.99f) newWeight = 0.99f;

            if (positiveCount > totalCount / 2)
                if (newWeight != currentWeight || r is null)
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
                        debugString += $"Added  {r.ToString()}   {r.Weight.ToString(".0")} \n";

                        foreach (Thing t1 in t.Children)
                        {
                            Relationship rrr = t1.RemoveRelationship(rr.target, rr.relType);
                            debugString += $"Removed {rrr.ToString()} \n";
                        }
                        //if there is a conflicting relationship, delete it
                        for (int j = 0; j < t.Relationships.Count; j++)
                        {
                            if (RelationshipsConflict(new RelDest(r), new RelDest(t.Relationships[j])))
                            {
                                t.RemoveRelationship(t.Relationships[j]);
                                j--;
                            }
                        }
                    }
                }
        }

    }


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
                if (parent.HasProperty("isExclusive") || parent.HasProperty("allowMultiple")) return true;
        }
        if (r1.target == r2.target)
        {
            var parents = FindCommonParents(r1.target, r2.target);
            foreach (var parent in parents)
                if (parent.HasProperty("isExclusive")) return true;

            //get the attributes of the relationships
            IList<Thing> r1RelAttribs = r1.relType.GetAttributes();
            IList<Thing> r2RelAttribs = r2.relType.GetAttributes();

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
                        if (t3.HasProperty("isexclusive") || t3.HasProperty("allowMultiple"))
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
    public static Thing GetInstanceType(Thing t)
    {
        bool EndsInInteger(string input)
        {
            // Regular expression to check if the string ends with a sequence of digits
            return Regex.IsMatch(input, @"\d+$");
        }
        Thing useRelType = t;
        while (useRelType.Parents.Count > 0 && EndsInInteger(useRelType.Label) && 
            !t.Label.Contains(".") && useRelType.Label.StartsWith(useRelType.Parents[0].Label))
            useRelType = useRelType.Parents[0];
        return useRelType;
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