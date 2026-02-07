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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleAddCounts : ModuleBase
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


    public void DoTheWork()
    {
        debugString = "Agent Started\n";
        for (int i = 0; i < theUKS.AllThoughts.Count; i++)
        {
            Thought t = theUKS.AllThoughts[i];
            AddCountLinks(t);
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }

    private void AddCountLinks(Thought t)
    {
        for (int j = 0; j < t.LinksTo.Count; j++)
        {
            Thought r = t.LinksTo[j];
            if (r.LinkType == Thought.IsA) continue;
            Thought useLinkType = ModuleAttributeBubble.GetInstanceType(r.LinkType);

            //get the counts of targets and/or their ancestors
            List<Thought> targets = t.LinksTo.FindAll(x => ModuleAttributeBubble.GetInstanceType(x.LinkType) == useLinkType).Select(x => x.To).ToList();
            List<(Thought tMatch, int bestCount)> bestMatches = GetAttributeCounts(targets);
            foreach (var match in bestMatches)
            {
                Thought existingLink = theUKS.GetLink(r.From, useLinkType.ToString() + "." + match.bestCount.ToString(), match.tMatch);
                if (existingLink is null)
                {
                    string newRelLabel = useLinkType.ToString() + "." + match.bestCount.ToString();
                    Thought newLinkType = theUKS.GetOrAddThought(newRelLabel, useLinkType.Parents[0]);
                    Thought rAdded = theUKS.AddStatement(r.From.Label, newLinkType, match.tMatch);
                    debugString += $"Added: {rAdded}\n";
                }
            }
        }
    }

    private List<(Thought, int)> GetAttributeCounts(List<Thought> ts)
    {
        List<(Thought, int)> retVal = new();
        if (ts.Count > 0)
        {
            Dictionary<Thought, int> dict = new();

            List<IReadOnlyList<Thought>> theAncestors = new();
            foreach (Thought t in ts)
            {
                if (t is null) continue;
                foreach (Thought t1 in t.AncestorList())
                {
                    if (dict.ContainsKey(t1))
                        dict[t1]++;
                    else
                        dict[t1] = 1;
                }
            }
            foreach (var k in dict.Keys)
            {
                if (!k.HasAncestor("Unknown") || k == (Thought)"Unknown") continue;
                if (dict[k] > 1)
                    retVal.Add((k, dict[k]));
            }
        }
        return retVal;
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