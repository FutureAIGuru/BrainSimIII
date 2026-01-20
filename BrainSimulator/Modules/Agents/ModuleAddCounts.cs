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
        for (int i = 0; i < theUKS.AllThings.Count; i++)
        {
            Cogneme t = theUKS.AllThings[i];
            AddCountRelationships(t);
        }
        debugString += "Agent  Finished\n";
        UpdateDialog();
    }

    private void AddCountRelationships(Cogneme t)
    {
        for (int j = 0; j < t.Relationships.Count; j++)
        {
            Cogneme r = t.Relationships[j];
            if (r.RelType == Cogneme.IsA) continue;
            Cogneme useRelType = ModuleAttributeBubble.GetInstanceType(r.RelType);

            //get the counts of targets and/or their ancestors
            List<Cogneme> targets = t.Relationships.FindAll(x => ModuleAttributeBubble.GetInstanceType(x.RelType) == useRelType).Select(x => x.Target).ToList();
            List<(Cogneme tMatch, int bestCount)> bestMatches = GetAttributeCounts(targets);
            foreach (var match in bestMatches)
            {
                Cogneme existingRelationship = theUKS.GetRelationship(r.Source, useRelType.ToString() + "." + match.bestCount.ToString(), match.tMatch);
                if (existingRelationship is null)
                {
                    string newRelLabel = useRelType.ToString() + "." + match.bestCount.ToString();
                    Cogneme newRelType = theUKS.GetOrAddThing(newRelLabel, useRelType.Parents[0]);
                    Cogneme rAdded = theUKS.AddStatement(r.Source.Label, newRelType, match.tMatch);
                    debugString += $"Added: {rAdded}\n";
                }
            }
        }
    }

    private List<(Cogneme, int)> GetAttributeCounts(List<Cogneme> ts)
    {
        List<(Cogneme, int)> retVal = new();
        if (ts.Count > 0)
        {
            Dictionary<Cogneme, int> dict = new();

            List<IReadOnlyList<Cogneme>> theAncestors = new();
            foreach (Cogneme t in ts)
            {
                if (t is null) continue;
                foreach (Cogneme t1 in t.AncestorList())
                {
                    if (dict.ContainsKey(t1))
                        dict[t1]++;
                    else
                        dict[t1] = 1;
                }
            }
            foreach (var k in dict.Keys)
            {
                if (!k.HasAncestor("unknownObject") || k == (Cogneme)"unknownObject") continue;
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