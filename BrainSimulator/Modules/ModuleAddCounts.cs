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
        for (int i = 0; i < theUKS.UKSList.Count; i++)
        {
            Thing t = theUKS.UKSList[i];
            AddCountRelationships(t);
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }

    private void AddCountRelationships(Thing t)
    {
        for (int j = 0; j < t.Relationships.Count; j++)
        {
            Relationship r = t.Relationships[j];
            if (r.reltype == Thing.HasChild) continue;
            Thing useRelType = ModuleAttributeBubble.GetInstanceType(r.reltype);

            //get the counts of targets and/or their ancestors
            List<Thing> targets = t.Relationships.FindAll(x => ModuleAttributeBubble.GetInstanceType(x.reltype) == useRelType).Select(x => x.target).ToList();
            List<(Thing tMatch, int bestCount)> bestMatches = GetAttributeCounts(targets);
            foreach (var match in bestMatches)
            {
                Relationship existingRelationship = theUKS.GetRelationship(r.source, useRelType.ToString() + "." + match.bestCount.ToString(), match.tMatch);
                if (existingRelationship == null)
                {
                    Relationship rAdded = theUKS.AddStatement(r.source, useRelType, match.tMatch, null, match.bestCount.ToString());
                    debugString += $"Added: {rAdded}\n";
                }
            }
        }
    }

    private List<(Thing, int)> GetAttributeCounts(List<Thing> ts)
    {
        List<(Thing, int)> retVal = new();
        if (ts.Count > 0)
        {
            Dictionary<Thing, int> dict = new();

            List<IList<Thing>> theAncestors = new();
            foreach (Thing t in ts)
            {
                foreach (Thing t1 in t.AncestorList())
                {
                    if (dict.ContainsKey(t1))
                        dict[t1]++;
                    else
                        dict[t1] = 1;
                }
            }
            foreach (var k in dict.Keys)
            {
                if (!k.HasAncestor("unknownObject") || k == (Thing)"unknownObject") continue;
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