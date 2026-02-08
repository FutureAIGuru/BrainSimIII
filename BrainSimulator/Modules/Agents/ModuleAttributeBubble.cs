/*
 * Brain Simulator Through
 *
 * Copyright (c) 2026 Charles Simon
 *
 * This file is part of Brain Simulator Through and is licensed under
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

    public class LinkDest
    {
        public Thought linkType;
        public Thought target;
        public List<Thought> links = new();
        public LinkDest()
        { }
        public LinkDest(Thought r)
        {
            linkType = r.LinkType;
            target = r.To;
            links.Add(r);
        }
        public override string ToString()
        {
            return $"{linkType.Label} -> {target.Label}  :  {links.Count}";
        }
    }

    public void DoTheWork()
    {
        debugString = "Bubbler Started\n";
        foreach (Thought t in theUKS.AllThoughts)
        {
            if (t.Label == "Animal")
            { }
            if (t.HasAncestor("Object"))
                BubbleChildAttributes(t);
        }
        debugString += "Bubbler Finished\n";
        UpdateDialog();
    }
    void BubbleChildAttributes(Thought t)
    {
        if (t.Children.Count == 0) return;
        if (t.Label == "Unknown") return;

        //build a List of all the Links which this thought's children have
        List<LinkDest> itemCounts = new();
        foreach (Thought t1 in t.ChildrenWithSubclasses)
        {
            foreach (Thought r in t1.LinksTo)
            {
                if (r.LinkType == Thought.IsA) continue;
                Thought useLinkType = GetInstanceType(r.LinkType);

                LinkDest foundItem = itemCounts.FindFirst(x => x.linkType == useLinkType && x.target == r.To);
                if (foundItem is null)
                {
                    foundItem = new LinkDest { linkType = useLinkType, target = r.To };
                    itemCounts.Add(foundItem);
                }
                foundItem.links.Add(r);
            }
        }
        if (itemCounts.Count == 0) return;
        var sortedItems = itemCounts.OrderByDescending(x => x.links.Count).ToList();

        List<string> excludeTypes = new List<string>() { "hasProperty", "isTransitive", "isCommutative", "inverseOf", "hasAttribute", "hasDigit" };
        //bubble the links
        for (int i = 0; i < sortedItems.Count; i++)
        {
            LinkDest rr = sortedItems[i];
            if (excludeTypes.Contains(rr.linkType.Label, comparer: StringComparer.OrdinalIgnoreCase)) continue;

            //find an existing link
            Thought r = theUKS.GetLink(t, rr.linkType, rr.target);
            float currentWeight = (r is not null) ? r.Weight : 0f;

            //We need 1) count for this Thought, 2) count for any conflicting, 3) count without a reference
            float totalCount = t.Children.Count;
            float positiveCount = rr.links.FindAll(x => x.Weight > .5f).Count;
            float positiveWeight = rr.links.Sum(x => x.Weight);
            float negativeCount = 0;
            float negativeWeight = 0;
            //are there any conflicting links
            for (int j = 0; j < sortedItems.Count; j++)
            {
                if (j == i) continue;
                if (LinksConflict(rr, sortedItems[j]))
                {
                    negativeCount += sortedItems[j].links.Count; //?  why not += 1
                    negativeWeight += sortedItems[j].links.Sum(x => x.Weight);
                }
            }
            float noInfoCount = totalCount - (positiveCount + negativeCount);
            positiveWeight += currentWeight + noInfoCount * 0.51f;
            if (noInfoCount < 0) noInfoCount = 0;

            if (negativeCount >= positiveCount)
            {
                if (r is not null)
                {
                    t.RemoveLink(r);
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
                        if (r is not null)
                        {
                            t.RemoveLink(r);
                            debugString += $"Removed {r.ToString()} \n";
                        }
                    }
                    else
                    {
                        //bubble the property
                        r = t.AddLink(rr.target, rr.linkType);
                        r.Weight = newWeight;
                        r.Fire();
                        debugString += $"Added  {r.ToString()}   {r.Weight.ToString(".0")} \n";

                        foreach (Thought t1 in t.Children)
                        {
                            Thought rrr = t1.RemoveLink(rr.target, rr.linkType);
                            debugString += $"Removed {rrr.ToString()} \n";
                        }
                        //if there is a conflicting link, delete it
                        for (int j = 0; j < t.LinksTo.Count; j++)
                        {
                            if (LinksConflict(new LinkDest(r), new LinkDest(t.LinksTo[j])))
                            {
                                t.RemoveLink(t.LinksTo[j]);
                                j--;
                            }
                        }
                    }
                }
        }

    }


    //If some links are exceptions, we can still bubble the 
    //Links are exceptions if they conflict AND numbers are one are small relative to the other.
    //a conflicting Thought is:
    //  linktypes are the same AND targets are different but have a common parent w/ isexclusive (colors)
    //  targets are the same AND linkTypes are different and have attributes with acommon parent which has the IsExslucive property (counts) (have 3, have 4)
    // Modified from UKS.CS line 181.  This does not includ AllowMultiples as these should not be bubbled
    private bool LinksConflict(LinkDest r1, LinkDest r2)
    {
        if (r1.linkType == r2.linkType && r1.target == r2.target) return false;
        if (r1.linkType == r2.linkType)
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

            //get the attributes of the links
            IReadOnlyList<Thought> r1RelAttribs = r1.linkType.GetAttributes();
            IReadOnlyList<Thought> r2RelAttribs = r2.linkType.GetAttributes();

            Thought r1Not = r1RelAttribs.FindFirst(x => x.Label == "not" || x.Label == "no");
            Thought r2Not = r2RelAttribs.FindFirst(x => x.Label == "not" || x.Label == "no");
            if (r1Not is null && r2Not is not null || r1Not is not null && r2Not is null)
                return true;

            //are any of the attrbutes which are exclusive?
            foreach (Thought t1 in r1RelAttribs)
                foreach (Thought t2 in r2RelAttribs)
                {
                    if (t1 == t2) continue;
                    List<Thought> commonParents = FindCommonParents(t1, t2);
                    foreach (Thought t3 in commonParents)
                    {
                        if (t3.HasProperty("isexclusive") || t3.HasProperty("allowMultiple"))
                            return true;
                    }
                }
            // handle special case where one linktype has is numberic and the other is not
            bool hasNumber1 = (r1RelAttribs.FindFirst(x => x.HasAncestor("number")) is not null);
            bool hasNumber2 = (r2RelAttribs.FindFirst(x => x.HasAncestor("number")) is not null);
            if (hasNumber1 || hasNumber2) return true;

        }
        return false;
    }


    private static List<Thought> FindCommonParents(Thought t, Thought t1)
    {
        //BORROWED from UKSStatement.cs line 323
        List<Thought> commonParents = new List<Thought>();
        foreach (Thought p in t.Parents)
            if (t1.Parents.Contains(p))
                commonParents.Add(p);
        return commonParents;
    }


    bool BubbleNeeded()
    {
        return true;
    }



    //if the given thought is an instance of its parent, get the parent
    public static Thought GetInstanceType(Thought t)
    {
        bool EndsInInteger(string input)
        {
            // Regular expression to check if the string ends with a sequence of digits
            return Regex.IsMatch(input, @"\d+$");
        }
        Thought useLinkType = t;
        while (useLinkType.Parents.Count > 0 && EndsInInteger(useLinkType.Label) && 
            !t.Label.Contains(".") && useLinkType.Label.StartsWith(useLinkType.Parents[0].Label))
            useLinkType = useLinkType.Parents[0];
        return useLinkType;
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