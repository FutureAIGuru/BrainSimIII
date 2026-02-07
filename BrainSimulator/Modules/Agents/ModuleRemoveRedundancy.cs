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
using System.Threading;
using UKS;

namespace BrainSimulator.Modules;

public class ModuleRemoveRedundancy : ModuleBase
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
        foreach (Thought t in theUKS.AllThoughts)
        {
            RemoveRedundantAttributes(t);
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }

    private void RemoveRedundantAttributes(Thought t)
    {
        foreach (Thought parent in t.Parents) //usually only a single parent
        {
            List<Thought> linksWithInheritance = theUKS.GetAllLinks(new List<Thought> { parent });
            for (int i = 0; i < t.LinksTo.Count; i++)
            {
                Thought r = t.LinksTo[i];
                Thought rMatch = linksWithInheritance.FindFirst(x => x.From != r.From && x.LinkType == r.LinkType && x.To == r.To);
                if (rMatch is not null && rMatch.Weight > 0.8f)
                {
                    r.Weight -= 0.1f;
                    if (r.Weight < 0.5f)
                    {
                        t.RemoveLink(r);
                        i--;
                        debugString += "Removed: ";
                    }
                    debugString += $"{r}   ({r.Weight:0.00})\n";
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