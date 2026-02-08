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

using System.Collections.Generic;
using System.Threading;
using UKS;
using static BrainSimulator.Modules.ModuleAttributeBubble;

namespace BrainSimulator.Modules;

public class ModuleClassCreate : ModuleBase
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
    private int maxChildren = 12;
    private int minCommonAttributes = 3;
    public int MaxChildren { get => maxChildren; set => maxChildren = value; }

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


    public void DoTheWork()
    {
        debugString = "Agent Started\n";
        for (int i = 0; i < theUKS.AllThoughts.Count; i++)
        {
            Thought t = theUKS.AllThoughts[i];
            if (t.HasAncestor("Object") && !t.Label.Contains(".") && !t.Label.Contains("unknown"))
            {
                HandleClassWithCommonAttributes(t);
            }
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }

    void HandleClassWithCommonAttributes(Thought t)
    {
        //build a List of counts of the attributes
        //build a List of all the Links which this thought's children have
        List<LinkDest> attributes = new();
        foreach (Thought t1 in t.ChildrenWithSubclasses)
        {
            foreach (Thought r in t1.LinksTo)
            {
                if (r.LinkType == Thought.IsA) continue;
                Thought useLinkType = GetInstanceType(r.LinkType);

                LinkDest foundItem = attributes.FindFirst(x => x.linkType == useLinkType && x.target == r.To);
                if (foundItem is null)
                {
                    foundItem = new LinkDest { linkType = useLinkType, target = r.To };
                    attributes.Add(foundItem);
                }
                if (foundItem.links.FindFirst(x=>x.From == r.From && x.To == r.To) is null)
                    foundItem.links.Add(r);
            }
        }
        //create intermediate parent Thoughts
        foreach (var key in attributes)
        {
            if (key.links.Count >= minCommonAttributes)
            {
                Thought newParent = theUKS.GetOrAddThought(t.Label + "." + key.linkType + "." + key.target, t);
                newParent.AddLink(key.target, key.linkType);
                debugString += "Created new subclass " + newParent;
                foreach (Thought r in key.links)
                {
                    Thought tChild = (Thought)r.From;
                    tChild.AddParent(newParent);
                    tChild.RemoveParent(t);
                }
            }
        }
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